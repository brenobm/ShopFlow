using Amazon;

namespace BrenoMachado.ShopFlow
{
    public static class AwsConfig
    {
        // TODO: Replace with your actual values after provisioning resources
        public const string UserPoolId = "us-west-2_mQ39tQDFE";
        public const string UserPoolClientId = "61fcskms6g0o2epem6lrbvge58";
        public const string IdentityPoolId = "us-west-2:4e930d96-c00e-4d93-aa32-8b0a2f3d76df"; // Required for DynamoDB access
        public static readonly RegionEndpoint Region = RegionEndpoint.USWest2;
        
        // DynamoDB Table Names
        public const string ItemsTableName = "ShopFlow_Items";
        public const string ListsTableName = "ShopFlow_Lists";
        public const string GraphqlEndpoint = "https://msd5yv5s7fdevbcgwt56giiyy4.appsync-api.us-west-2.amazonaws.com/graphql";
    }
}
