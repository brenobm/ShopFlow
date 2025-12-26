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
