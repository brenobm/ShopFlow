using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;
using Amazon.CDK.AWS.Cognito.IdentityPool.Alpha;
using System.Collections.Generic;

namespace Backend
{
    public class BackendStack : Stack
    {
        internal BackendStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // 1. User Pool
            var userPool = new UserPool(this, "ShopFlowUserPool", new UserPoolProps
            {
                UserPoolName = "ShopFlowUserPool",
                SelfSignUpEnabled = true,
                SignInAliases = new SignInAliases { Email = true, Username = true },
                AutoVerify = new AutoVerifiedAttrs { Email = true },
                RemovalPolicy = RemovalPolicy.DESTROY // For dev/demo only. In prod change to RETAIN.
            });

            // 2. User Pool Client
            var userPoolClient = userPool.AddClient("ShopFlowClient", new UserPoolClientProps
            {
                UserPoolClientName = "ShopFlowClient",
                AuthFlows = new AuthFlow
                {
                    UserSrp = true
                }
            });

            // 3. Identity Pool (Alpha L2)
            var identityPool = new IdentityPool(this, "ShopFlowIdentityPool", new IdentityPoolProps
            {
                IdentityPoolName = "ShopFlowIdentityPool",
                AllowUnauthenticatedIdentities = false, 
                AuthenticationProviders = new IdentityPoolAuthenticationProviders
                {
                    UserPools = new IUserPoolAuthenticationProvider[] 
                    { 
                        new UserPoolAuthenticationProvider(new UserPoolAuthenticationProviderProps
                        {
                            UserPool = userPool,
                            UserPoolClient = userPoolClient
                        }) 
                    }
                }
            });

            // 4. DynamoDB Table
            var table = new Table(this, "ShopFlowItems", new TableProps
            {
                TableName = "ShopFlow_Items",
                PartitionKey = new Attribute { Name = "UserId", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "Id", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // 5. Grant Access
            // Identity Pool Authenticated Role needs access to the table.
            // The IdentityPool construct automatically creates an AuthenticatedRole.
            table.GrantReadWriteData(identityPool.AuthenticatedRole);

            // 6. Outputs
            new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = userPool.UserPoolId });
            new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = userPoolClient.UserPoolClientId });
            new CfnOutput(this, "IdentityPoolId", new CfnOutputProps { Value = identityPool.IdentityPoolId });
            new CfnOutput(this, "Region", new CfnOutputProps { Value = this.Region });
            new CfnOutput(this, "ItemsTableName", new CfnOutputProps { Value = table.TableName });
        }
    }
}
