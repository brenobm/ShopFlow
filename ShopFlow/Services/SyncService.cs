using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BrenoMachado.ShopFlow.Models;
using System.Diagnostics;

namespace BrenoMachado.ShopFlow.Services
{
    public class SyncService : ISyncService
    {
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;
        private AmazonDynamoDBClient _dynamoClient;

        public SyncService(IAuthService authService, IDatabaseService databaseService)
        {
            _authService = authService;
            _databaseService = databaseService;
        }

        private async Task InitClient()
        {
            if (_dynamoClient != null) return;
            
            // Use CognitoAWSCredentials to get temporary AWS credentials for the logged-in user context
            // Note: This requires the Identity Pool to be configured to accept the specific User Pool as a provider.
            var credentials = new Amazon.CognitoIdentity.CognitoAWSCredentials(
                AwsConfig.IdentityPoolId, // Identity Pool ID
                AwsConfig.Region // Region
            );

            // Add the User Pool login to the credentials map
            var idToken = _authService.GetIdToken();
            if (!string.IsNullOrEmpty(idToken))
            {
                credentials.AddLogin($"cognito-idp.{AwsConfig.Region.SystemName}.amazonaws.com/{AwsConfig.UserPoolId}", idToken);
            }
            
            _dynamoClient = new AmazonDynamoDBClient(credentials, AwsConfig.Region);
        }

        public async Task SyncAsync()
        {
            await PushChangesAsync();
            await PullChangesAsync();
        }

        public async Task PullChangesAsync()
        {
            if (!await _authService.IsAuthenticatedAsync()) return;
            await InitClient();

            try
            {
                var userId = _authService.GetCurrentUserId();
                var request = new QueryRequest
                {
                    TableName = AwsConfig.ItemsTableName,
                    KeyConditionExpression = "UserId = :uid",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                        {":uid", new AttributeValue { S = userId }}
                    }
                };

                var response = await _dynamoClient.QueryAsync(request);
                
                foreach (var item in response.Items)
                {
                    await ProcessCloudItemAsync(item);
                }
            }
            catch (global::Amazon.DynamoDBv2.Model.ResourceNotFoundException)
            {
                 FileLoggerService.LogStatic($"[SyncService] CRITICAL: Table '{AwsConfig.ItemsTableName}' not found. Pull skipped.");
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"Sync Pull Error: {ex.Message}");
            }
        }

        private async Task ProcessCloudItemAsync(Dictionary<string, AttributeValue> cloudItem)
        {
             try 
             {
                 // Check for "id" (lowercase) as per Schema, fallback to "Id" just in case legacy data exists
                 string idKey = cloudItem.ContainsKey("id") ? "id" : (cloudItem.ContainsKey("Id") ? "Id" : null);
                 if (idKey == null) return; 

                 string id = cloudItem[idKey].S;
                 var localItem = await _databaseService.GetItemAsync(id);
                 
                 // Map Cloud Item to Model
                 var newItem = new ShoppingItem
                 {
                     Id = id,
                     Name = cloudItem["Name"].S,
                     IsPurchased = cloudItem["IsPurchased"].BOOL ?? false,
                     Category = cloudItem.ContainsKey("Category") ? cloudItem["Category"].S : "Uncategorized",
                     ListId = cloudItem.ContainsKey("listId") ? cloudItem["listId"].S : "default",
                     IsDirty = false, // Clean from cloud
                     IsDeleted = false
                 };

                 if (cloudItem.ContainsKey("UpdatedAt") && DateTime.TryParse(cloudItem["UpdatedAt"].S, out var cloudDate))
                 {
                     newItem.LastUpdated = cloudDate;
                 }
                 
                 // ... (Conflict resolution logic remains same)
                 if (localItem != null)
                 {
                     if (localItem.IsDirty)
                     {
                         if(localItem.LastUpdated > newItem.LastUpdated) return; 
                     }
                 }

                 await _databaseService.UpdateItemSyncStatusAsync(newItem);
             }
             catch(Exception ex)
             {
                 FileLoggerService.LogStatic($"Error processing cloud item: {ex.Message}");
             }
        }

        public async Task PushChangesAsync()
        {
            if (!await _authService.IsAuthenticatedAsync()) return;
            await InitClient();
            
            var dirtyItems = await _databaseService.GetDirtyItemsAsync();
            if (!dirtyItems.Any()) return;

            foreach (var item in dirtyItems)
            {
                bool success = false;
                 try
                {
                    if (item.IsDeleted)
                    {
                        await DeleteItemOnCloudAsync(item);
                        success = true;
                    }
                    else
                    {
                        await PushItemToCloudAsync(item);
                        success = true;
                    }
                }
                catch (global::Amazon.DynamoDBv2.Model.ResourceNotFoundException)
                {
                    FileLoggerService.LogStatic($"[SyncService] CRITICAL: Table '{AwsConfig.ItemsTableName}' not found.");
                    success = false; 
                    return; 
                }
                catch (Exception ex)
                {
                    FileLoggerService.LogStatic($"Sync Push Error for {item.Name}: {ex.Message}");
                }

                if (success)
                {
                    item.IsDirty = false;
                    await _databaseService.UpdateItemSyncStatusAsync(item);
                }
            }
        }

        private async Task PushItemToCloudAsync(ShoppingItem item)
        {
             var userId = _authService.GetCurrentUserId();
             if (string.IsNullOrEmpty(userId)) return;

             var attributeMap = new Dictionary<string, AttributeValue>
             {
                 { "userId", new AttributeValue { S = userId } },
                 { "id", new AttributeValue { S = item.Id } }, // Lowercase id
                 { "Name", new AttributeValue { S = item.Name } },
                 { "IsPurchased", new AttributeValue { BOOL = item.IsPurchased } },
                 { "Category", new AttributeValue { S = item.Category ?? "" } },
                 { "listId", new AttributeValue { S = item.ListId ?? "default" } },
                 { "UpdatedAt", new AttributeValue { S = (item.LastUpdated ?? DateTime.UtcNow).ToString("O") } }
             };
             
             var request = new PutItemRequest
             {
                 TableName = AwsConfig.ItemsTableName,
                 Item = attributeMap
             };
             await _dynamoClient.PutItemAsync(request);
        }

        private async Task DeleteItemOnCloudAsync(ShoppingItem item)
        {
             var userId = _authService.GetCurrentUserId();
             var request = new DeleteItemRequest
             {
                 TableName = AwsConfig.ItemsTableName,
                 Key = new Dictionary<string, AttributeValue> {
                     { "userId", new AttributeValue { S = userId } },
                     { "id", new AttributeValue { S = item.Id } } // Lowercase id
                 }
             };
             await _dynamoClient.DeleteItemAsync(request);
        }

        public async Task DeleteItemAsync(string itemId)
        {
             if (!await _authService.IsAuthenticatedAsync()) return;
            await InitClient();

             try
            {
                var userId = _authService.GetCurrentUserId();
                var request = new DeleteItemRequest
                {
                    TableName = AwsConfig.ItemsTableName,
                    Key = new Dictionary<string, AttributeValue> {
                        { "userId", new AttributeValue { S = userId } },
                        { "id", new AttributeValue { S = itemId } } // Lowercase id
                    }
                };

                await _dynamoClient.DeleteItemAsync(request);
            }
             catch (Exception ex)
            {
                FileLoggerService.LogStatic($"DeleteItem Error: {ex.Message}");
            }
        }
    }
}
