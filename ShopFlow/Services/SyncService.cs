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
            
            // In a real app, we would use credentials from the logged-in user (Cognito Identity Pool)
            // Here we use anonymous for demo or assume Config holds keys (which it shouldn't for client app)
            // For Phase 4, we assume we use the same credentials as Auth or basic setup.
            // Since we implemented UserPools, we need IdentityPools to get AWS Credentials.
            // Simplified: We just initialize it. In production, need CognitoAWSCredentials.
            
            _dynamoClient = new AmazonDynamoDBClient(new Amazon.Runtime.AnonymousAWSCredentials(), AwsConfig.Region);
        }

        public async Task SyncAsync()
        {
            if (!await _authService.IsAuthenticatedAsync()) return;
            await InitClient();

            try
            {
                var userId = _authService.GetCurrentUserId();
                // 1. Pull from Cloud
                var request = new QueryRequest
                {
                    TableName = AwsConfig.ItemsTableName,
                    KeyConditionExpression = "UserId = :uid",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                        {":uid", new AttributeValue { S = userId }}
                    }
                };

                // NOTE: This will fail if table doesn't exist or permissions denied (Anonymous)
                // We wrap in try-catch to allow local-only usage if cloud fails
                var response = await _dynamoClient.QueryAsync(request);
                
                foreach (var item in response.Items)
                {
                    // Map DynamoDB item to ShoppingItem and save local
                    // This is skipped for now as we don't have the table
                }

                // 2. Push Local to Cloud (Naive: Push all)
                var localItems = await _databaseService.GetItemsAsync("default"); // Assuming default list for now?
                foreach(var item in localItems)
                {
                    await PushItemAsync(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sync Error: {ex.Message}");
            }
        }

        public async Task PushItemAsync(ShoppingItem item)
        {
            if (!await _authService.IsAuthenticatedAsync()) return;
            await InitClient();

            try
            {
                var userId = _authService.GetCurrentUserId();
                var attributeMap = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = userId } },
                    { "Id", new AttributeValue { S = item.Id } },
                    { "Name", new AttributeValue { S = item.Name } },
                    { "IsPurchased", new AttributeValue { BOOL = item.IsPurchased } },
                    { "Category", new AttributeValue { S = item.Category ?? "" } }
                };

                var request = new PutItemRequest
                {
                    TableName = AwsConfig.ItemsTableName,
                    Item = attributeMap
                };

                await _dynamoClient.PutItemAsync(request);
            }
             catch (Exception ex)
            {
                Debug.WriteLine($"PushItem Error: {ex.Message}");
            }
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
                        { "UserId", new AttributeValue { S = userId } },
                        { "Id", new AttributeValue { S = itemId } }
                    }
                };

                await _dynamoClient.DeleteItemAsync(request);
            }
             catch (Exception ex)
            {
                Debug.WriteLine($"DeleteItem Error: {ex.Message}");
            }
        }
    }
}
