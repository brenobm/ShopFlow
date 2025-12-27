#pragma warning disable CS0612, CS0618
using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;
using Amazon.CDK.AWS.Cognito.IdentityPool.Alpha;
using System.Collections.Generic;
using Amazon.CDK.AWS.AppSync;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SES;
using System.IO;

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
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // 2. User Pool Client
            var userPoolClient = userPool.AddClient("ShopFlowClient", new UserPoolClientProps
            {
                UserPoolClientName = "ShopFlowClient",
                AuthFlows = new AuthFlow { UserSrp = true }
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

            // 4. DynamoDB Table (Items)
            var itemsTable = new Table(this, "ShopFlowItems", new TableProps
            {
                TableName = "ShopFlow_Items_v2",
                PartitionKey = new Attribute { Name = "userId", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "id", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY
            });
            
            // 5. DynamoDB Table (Lists - New)
             var listsTable = new Table(this, "ShopFlowLists", new TableProps
            {
                TableName = "ShopFlow_Lists",
                PartitionKey = new Attribute { Name = "id", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                Stream = StreamViewType.NEW_AND_OLD_IMAGES,
                RemovalPolicy = RemovalPolicy.DESTROY
            });


            // 6. AppSync API
            var api = new GraphqlApi(this, "ShopFlowApi", new GraphqlApiProps
            {
                Name = "ShopFlowApi",
                Schema = SchemaFile.FromAsset("src/Backend/schema.graphql"),
                AuthorizationConfig = new AuthorizationConfig
                {
                    DefaultAuthorization = new AuthorizationMode
                    {
                        AuthorizationType = AuthorizationType.USER_POOL,
                        UserPoolConfig = new UserPoolConfig
                        {
                            UserPool = userPool
                        }
                    }
                }
            });

            // Data Sources
            var itemsDS = api.AddDynamoDbDataSource("ItemsDataSource", itemsTable);
            var listsDS = api.AddDynamoDbDataSource("ListsDataSource", listsTable);

            // Resolvers (Basic Mapping)
            // Function to create standard request/response templates would be cleaner, but inlining for simplicity
            
            // LISTS Mapping
            // getLists -> Scan with Filter (Security Fix)
            listsDS.CreateResolver("GetListsResolver", new ResolverProps 
            {
                TypeName = "Query",
                FieldName = "getLists",
                RequestMappingTemplate = MappingTemplate.FromString(@"
                {
                    ""version"" : ""2017-02-28"",
                    ""operation"" : ""Scan"",
                    ""filter"" : {
                        ""expression"" : ""UserId = :userId OR contains(members, :email)"",
                        ""expressionValues"" : {
                            "":userId"" : { ""S"" : ""${context.identity.username}"" },
                            "":email"" : { ""S"" : ""${context.identity.claims.email}"" }
                        }
                    }
                }"),
                ResponseMappingTemplate = MappingTemplate.DynamoDbResultList()
            });

            // ITEMS Mapping
            // getItems(listId: ID!) -> Query where userId = $ctx.identity.username AND filter listId = $args.listId
            // Note: Ideally we should allow members to get items too, but for now we trust they have the ListId from getLists
            itemsDS.CreateResolver("GetItemsResolver", new ResolverProps
            {
                TypeName = "Query",
                FieldName = "getItems",
                RequestMappingTemplate = MappingTemplate.FromString(@"
                {
                    ""version"" : ""2017-02-28"",
                    ""operation"" : ""Query"",
                    ""query"" : {
                        ""expression"" : ""userId = :userId"",
                        ""expressionValues"" : {
                            "":userId"" : { ""S"" : ""${context.identity.username}"" }
                        }
                    },
                    ""filter"" : {
                        ""expression"" : ""listId = :listId"",
                        ""expressionValues"" : {
                             "":listId"" : { ""S"" : ""${context.arguments.listId}"" }
                        }
                    }
                }"),
                ResponseMappingTemplate = MappingTemplate.DynamoDbResultList()
            });
            
            listsDS.CreateResolver("CreateListResolver", new ResolverProps 
            {
                TypeName = "Mutation",
                FieldName = "createList",
                RequestMappingTemplate = MappingTemplate.FromString(@"
                {
                    ""version"" : ""2017-02-28"",
                    ""operation"" : ""PutItem"",
                    ""key"" : {
                        ""id"" : { ""S"" : ""$util.autoId()"" }
                    },
                    ""attributeValues"" : {
                        ""name"" : { ""S"" : ""${context.arguments.name}"" },
                        ""createdAt"" : { ""S"" : ""$util.time.nowISO8601()"" },
                        ""UserId"" : { ""S"" : ""${context.identity.username}"" },
                        ""members"" : { ""L"" : [] }
                    }
                }"),
                ResponseMappingTemplate = MappingTemplate.DynamoDbResultItem() 
            });

            // SHARE LIST Mapping
            listsDS.CreateResolver("ShareListResolver", new ResolverProps
            {
                TypeName = "Mutation",
                FieldName = "shareList",
                RequestMappingTemplate = MappingTemplate.FromString(@"
                {
                    ""version"" : ""2017-02-28"",
                    ""operation"" : ""UpdateItem"",
                    ""key"" : {
                        ""id"" : { ""S"" : ""${context.arguments.id}"" }
                    },
                    ""update"" : {
                        ""expression"" : ""SET members = list_append(if_not_exists(members, :empty_list), :new_member)"",
                        ""expressionValues"" : {
                            "":new_member"" : { ""L"" : [ { ""S"" : ""${context.arguments.email}"" } ] },
                            "":empty_list"" : { ""L"" : [] }
                        }
                    },
                    ""condition"": {
                        ""expression"": ""UserId = :currentUser"",
                        ""expressionValues"": {
                            "":currentUser"": { ""S"" : ""${context.identity.username}"" }
                        }
                    }
                }"),
                ResponseMappingTemplate = MappingTemplate.DynamoDbResultItem()
            });

            // ADD ITEM Mapping
            itemsDS.CreateResolver("AddItemResolver", new ResolverProps
            {
                TypeName = "Mutation",
                FieldName = "addItem",
                RequestMappingTemplate = MappingTemplate.FromString(@"
                {
                    ""version"" : ""2017-02-28"",
                    ""operation"" : ""PutItem"",
                    ""key"" : {
                        ""userId"" : { ""S"" : ""${context.identity.username}"" },
                        ""id"" : { ""S"" : ""$util.autoId()"" }
                    },
                    ""attributeValues"" : {
                        ""listId"" : { ""S"" : ""${context.arguments.listId}"" },
                        ""name"" : { ""S"" : ""${context.arguments.name}"" },
                        ""quantity"" : { ""N"" : ""${context.arguments.quantity}"" },
                        ""unit"" : { ""S"" : ""${context.arguments.unit}"" },
                        ""category"" : { ""S"" : ""${context.arguments.category}"" },
                        ""isPurchased"" : { ""BOOL"" : false }
                    }
                }"),
                ResponseMappingTemplate = MappingTemplate.DynamoDbResultItem()
            });

            // UPDATE ITEM Mapping
            // Note: Supports updating isPurchased, name, etc.
            itemsDS.CreateResolver("UpdateItemResolver", new ResolverProps
            {
                TypeName = "Mutation",
                FieldName = "updateItem",
                RequestMappingTemplate = MappingTemplate.FromString(@"
                {
                    ""version"" : ""2017-02-28"",
                    ""operation"" : ""UpdateItem"",
                    ""key"" : {
                        ""userId"" : { ""S"" : ""${context.identity.username}"" },
                        ""id"" : { ""S"" : ""${context.arguments.id}"" }
                    },
                    ""update"" : {
                        ""expression"" : ""SET #isPurchased = :isPurchased#if($context.arguments.name), #name = :name#end"",
                        ""expressionNames"" : {
                            ""#isPurchased"" : ""isPurchased""
                            #if($context.arguments.name),""#name"" : ""name""#end
                        },
                        ""expressionValues"" : {
                            "":isPurchased"" : { ""BOOL"" : ${context.arguments.isPurchased} }
                            #if($context.arguments.name),"":name"" : { ""S"" : ""${context.arguments.name}"" }#end
                        }
                    },
                    ""condition"": {
                        ""expression"": ""attribute_exists(id)""
                    }
                }"),
                ResponseMappingTemplate = MappingTemplate.DynamoDbResultItem()
            });

            // DELETE ITEM Mapping
            itemsDS.CreateResolver("DeleteItemResolver", new ResolverProps
            {
                TypeName = "Mutation",
                FieldName = "deleteItem",
                RequestMappingTemplate = MappingTemplate.FromString(@"
                {
                    ""version"" : ""2017-02-28"",
                    ""operation"" : ""DeleteItem"",
                    ""key"" : {
                        ""userId"" : { ""S"" : ""${context.identity.username}"" },
                        ""id"" : { ""S"" : ""${context.arguments.id}"" }
                    }
                }"),
                ResponseMappingTemplate = MappingTemplate.DynamoDbResultItem()
            });
             
            // 7. Lambda (Smart Suggestions)
            var suggestionsLambda = new Function(this, "SmartSuggestionsFunction", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Code = Amazon.CDK.AWS.Lambda.Code.FromAsset("src/Lambdas/SmartSuggestions/SmartSuggestions/src/SmartSuggestions/bin/Release/net8.0/linux-x64/publish"), // Assumes user ran dotnet publish
                Handler = "SmartSuggestions::SmartSuggestions.Function::FunctionHandler",
                Timeout = Duration.Seconds(30)
            });
            
            // Grant Lambda access to tables
            itemsTable.GrantReadData(suggestionsLambda);

            // 8. SES Identity (Email)
            // Note: This sends a verification email to the user.
            var emailIdentity = new EmailIdentity(this, "Identity", new EmailIdentityProps
            {
                Identity = Identity.Email("breno.machado@example.com") // TODO: Replace with user email or ask input
            });

            // 9. Invite User Lambda (DynamoDB Stream)
            var inviteUserLambda = new Function(this, "InviteUserFunction", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Code = Amazon.CDK.AWS.Lambda.Code.FromAsset("src/Lambdas/InviteUser/publish"),
                Handler = "InviteUser::InviteUser.Function::FunctionHandler",
                Timeout = Duration.Seconds(30),
                 Environment = new Dictionary<string, string>
                {
                    { "fEMAIL_SOURCE", "breno.machado@example.com" }
                }
            });

            inviteUserLambda.AddEventSource(new DynamoEventSource(listsTable, new DynamoEventSourceProps
            {
                StartingPosition = StartingPosition.LATEST,
                BatchSize = 1,
            }));
            
            // Permissions
            // Allow Lambda to read from Stream (handled by AddEventSource usually, but explicit read is good)
            listsTable.GrantStreamRead(inviteUserLambda);
            
            // Allow Lambda to Send Email
            // emailIdentity.GrantSendEmail(inviteUserLambda); // L2 method might not exist in all versions or is strictly identity policy
            // Using generic IAM for simplicity/compatibility
            inviteUserLambda.AddToRolePolicy(new Amazon.CDK.AWS.IAM.PolicyStatement(new Amazon.CDK.AWS.IAM.PolicyStatementProps
            {
                Actions = new[] { "ses:SendEmail", "ses:SendRawEmail" },
                Resources = new[] { "*" } // In prod, restrict to specific identity ARN
            }));

            // 10. Grant Access to Identity Pool
            itemsTable.GrantReadWriteData(identityPool.AuthenticatedRole);
            listsTable.GrantReadWriteData(identityPool.AuthenticatedRole);

            // 10. Outputs
            new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = userPool.UserPoolId });
            new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = userPoolClient.UserPoolClientId });
            new CfnOutput(this, "IdentityPoolId", new CfnOutputProps { Value = identityPool.IdentityPoolId });
            new CfnOutput(this, "Region", new CfnOutputProps { Value = this.Region });
            new CfnOutput(this, "ItemsTableName", new CfnOutputProps { Value = itemsTable.TableName });
            new CfnOutput(this, "ListsTableName", new CfnOutputProps { Value = listsTable.TableName });
            new CfnOutput(this, "GraphQLEndpoint", new CfnOutputProps { Value = api.GraphqlUrl });
        }
    }
}
