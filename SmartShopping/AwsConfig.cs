using Amazon;

namespace BrenoMachado.ShopFlow
{
    public static class AwsConfig
    {
        // TODO: Replace with your actual values after provisioning resources
        public const string UserPoolId = "us-east-1_xxxxxxxxx";
        public const string UserPoolClientId = "xxxxxxxxxxxxxxxxxxxxxxxxxx";
        public static readonly RegionEndpoint Region = RegionEndpoint.USEast1;
        
        // DynamoDB Table Names
        public const string ItemsTableName = "SmartShopping_Items";
        public const string ListsTableName = "SmartShopping_Lists";
    }
}
