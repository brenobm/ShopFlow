using Amazon;

namespace BrenoMachado.ShopFlow
{
    public static class AwsConfig
    {
        // TODO: Replace with your actual values after provisioning resources
        public const string UserPoolId = "us-east-1_xxxxxxxxx";
        public const string UserPoolClientId = "xxxxxxxxxxxxxxxxxxxxxxxxxx";
        public const string IdentityPoolId = "us-east-1:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"; // Required for DynamoDB access
        public static readonly RegionEndpoint Region = RegionEndpoint.USEast1;
        
        // DynamoDB Table Names
        public const string ItemsTableName = "ShopFlow_Items";
        public const string ListsTableName = "ShopFlow_Lists";
    }
}
