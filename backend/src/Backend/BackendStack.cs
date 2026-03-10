#pragma warning disable CS0612, CS0618
using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;
using Amazon.CDK.AWS.IAM;
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
            // Parameters
            var senderEmail = new CfnParameter(this, "SenderEmail", new CfnParameterProps
            {
                Type = "String",
                Description = "The email address to send invitations from. Must be verified in SES.",
                Default = "breno.machado@example.com"
            });

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

            // 3. Identity Pool (L1 Construct)
            var identityPool = new CfnIdentityPool(this, "ShopFlowIdentityPool", new CfnIdentityPoolProps
            {
                IdentityPoolName = "ShopFlowIdentityPool",
                AllowUnauthenticatedIdentities = false,
                CognitoIdentityProviders = new[]
                {
                    new CfnIdentityPool.CognitoIdentityProviderProperty
                    {
                        ClientId = userPoolClient.UserPoolClientId,
                        ProviderName = userPool.UserPoolProviderName
                    }
                }
            });

            var authenticatedRole = new Role(this, "CognitoDefaultAuthenticatedRole", new RoleProps
            {
                AssumedBy = new FederatedPrincipal("cognito-identity.amazonaws.com", new Dictionary<string, object>
                {
                    { "StringEquals", new Dictionary<string, string> { { "cognito-identity.amazonaws.com:aud", identityPool.Ref } } },
                    { "ForAnyValue:StringLike", new Dictionary<string, string> { { "cognito-identity.amazonaws.com:amr", "authenticated" } } }
                }, "sts:AssumeRoleWithWebIdentity")
            });

            new CfnIdentityPoolRoleAttachment(this, "IdentityPoolRoleAttachment", new CfnIdentityPoolRoleAttachmentProps
            {
                IdentityPoolId = identityPool.Ref,
                Roles = new Dictionary<string, string>
                {
                    { "authenticated", authenticatedRole.RoleArn }
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

            // 5.1. DynamoDB Table (Global Products)
            var globalProductsTable = new Table(this, "ShopFlowGlobalProducts", new TableProps
            {
                TableName = "ShopFlow_GlobalProducts",
                PartitionKey = new Attribute { Name = "barcode", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // 5.2. DynamoDB Table (Device Tokens - New for Notifications)
            var deviceTokensTable = new Table(this, "ShopFlowDeviceTokens", new TableProps
            {
                TableName = "ShopFlow_DeviceTokens",
                PartitionKey = new Attribute { Name = "userId", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            deviceTokensTable.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "EmailIndex",
                PartitionKey = new Attribute { Name = "email", Type = AttributeType.STRING },
                ProjectionType = ProjectionType.ALL
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
            var globalProductsDS = api.AddDynamoDbDataSource("GlobalProductsDataSource", globalProductsTable);
            var deviceTokensDS = api.AddDynamoDbDataSource("DeviceTokensDataSource", deviceTokensTable);

            // GLOBAL PRODUCTS Mappping
            // getGlobalProduct
            globalProductsDS.CreateResolver("GetGlobalProductResolver", new ResolverProps
            {
                 TypeName = "Query",
                 FieldName = "getGlobalProduct",
                 RequestMappingTemplate = MappingTemplate.FromString(@"
                 {
                    ""version"": ""2017-02-28"",
                    ""operation"": ""GetItem"",
                    ""key"": {
                        ""barcode"": { ""S"": ""${context.arguments.barcode}"" }
                    }
                 }"),
                 ResponseMappingTemplate = MappingTemplate.DynamoDbResultItem()
            });

            // createGlobalProduct
            globalProductsDS.CreateResolver("CreateGlobalProductResolver", new ResolverProps
            {
                 TypeName = "Mutation",
                 FieldName = "createGlobalProduct",
                 RequestMappingTemplate = MappingTemplate.FromString(@"
                 {
                    ""version"": ""2017-02-28"",
                    ""operation"": ""PutItem"",
                    ""key"": {
                        ""barcode"": { ""S"": ""${context.arguments.barcode}"" }
                    },
                    ""attributeValues"": {
                        ""name"": { ""S"": ""${context.arguments.name}"" },
                        ""category"": { ""S"": ""${context.arguments.category}"" }
                    }
                 }"),
                 ResponseMappingTemplate = MappingTemplate.DynamoDbResultItem()
            });

            // registerDevice
            deviceTokensDS.CreateResolver("RegisterDeviceResolver", new ResolverProps
            {
                 TypeName = "Mutation",
                 FieldName = "registerDevice",
                 RequestMappingTemplate = MappingTemplate.FromString(@"
                 {
                    ""version"": ""2017-02-28"",
                    ""operation"": ""PutItem"",
                    ""key"": {
                        ""userId"": { ""S"": ""${context.identity.username}"" }
                    },
                    ""attributeValues"": {
                        ""token"": { ""S"": ""${context.arguments.token}"" },
                        ""platform"": { ""S"": ""${context.arguments.platform}"" },
                        ""email"": { ""S"": ""${context.identity.claims.email}"" },
                        ""updatedAt"": { ""S"": ""$util.time.nowISO8601()"" }
                    }
                 }"),
                 ResponseMappingTemplate = MappingTemplate.DynamoDbResultItem()
            });

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
                        ""members"" : { ""L"" : [] },
                        ""isDefault"" : { ""BOOL"" : ${context.arguments.isDefault} }
                        #if($context.arguments.color), ""color"" : { ""S"" : ""${context.arguments.color}"" } #end
                        #if($context.arguments.icon), ""icon"" : { ""S"" : ""${context.arguments.icon}"" } #end
                    }
                }"),
                ResponseMappingTemplate = MappingTemplate.DynamoDbResultItem() 
            });

             // UPDATE LIST Mapping
            listsDS.CreateResolver("UpdateListResolver", new ResolverProps
            {
                TypeName = "Mutation",
                FieldName = "updateList",
                RequestMappingTemplate = MappingTemplate.FromString(@"
                {
                    ""version"" : ""2017-02-28"",
                    ""operation"" : ""UpdateItem"",
                    ""key"" : {
                        ""id"" : { ""S"" : ""${context.arguments.id}"" }
                    },
                    ""update"" : {
                        ""expression"" : ""SET #name = :name, #isDefault = :isDefault #if($context.arguments.color), #color = :color #end #if($context.arguments.icon), #icon = :icon #end"",
                         ""expressionNames"" : {
                            ""#name"" : ""name"",
                            ""#isDefault"" : ""isDefault""
                            #if($context.arguments.color), ""#color"" : ""color"" #end
                            #if($context.arguments.icon), ""#icon"" : ""icon"" #end
                        },
                        ""expressionValues"" : {
                            "":name"" : { ""S"" : ""${context.arguments.name}"" },
                            "":isDefault"" : { ""BOOL"" : ${context.arguments.isDefault} }
                            #if($context.arguments.color), "":color"" : { ""S"" : ""${context.arguments.color}"" } #end
                            #if($context.arguments.icon), "":icon"" : { ""S"" : ""${context.arguments.icon}"" } #end
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

            // DELETE LIST Mapping
            listsDS.CreateResolver("DeleteListResolver", new ResolverProps
            {
                TypeName = "Mutation",
                FieldName = "deleteList",
                RequestMappingTemplate = MappingTemplate.FromString(@"
                {
                    ""version"" : ""2017-02-28"",
                    ""operation"" : ""DeleteItem"",
                    ""key"" : {
                        ""id"" : { ""S"" : ""${context.arguments.id}"" }
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
                        ""id"" : { ""S"" : ""#if($context.arguments.id)${context.arguments.id}#else$util.autoId()#end"" }
                    },
                    ""attributeValues"" : {
                        ""listId"" : { ""S"" : ""${context.arguments.listId}"" },
                        ""name"" : { ""S"" : ""${context.arguments.name}"" },
                        ""quantity"" : { ""N"" : ""${context.arguments.quantity}"" },
                        ""unit"" : { ""S"" : ""${context.arguments.unit}"" },
                        ""category"" : { ""S"" : ""${context.arguments.category}"" }#if($context.arguments.price),
                        ""price"" : { ""N"" : ""${context.arguments.price}"" }#end,
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
                        ""expression"" : ""SET #isPurchased = :isPurchased#if($context.arguments.name), #name = :name#end#if($context.arguments.price), #price = :price#end"",
                        ""expressionNames"" : {
                            ""#isPurchased"" : ""isPurchased""
                            #if($context.arguments.name),""#name"" : ""name""#end
                            #if($context.arguments.price),""#price"" : ""price""#end
                        },
                        ""expressionValues"" : {
                            "":isPurchased"" : { ""BOOL"" : ${context.arguments.isPurchased} }
                            #if($context.arguments.name),"":name"" : { ""S"" : ""${context.arguments.name}"" }#end
                            #if($context.arguments.price),"":price"" : { ""N"" : ""${context.arguments.price}"" }#end
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
                Identity = Identity.Email(senderEmail.ValueAsString)
            });

            /* TEMPORARILY DISABLED PINPOINT TO FIX DEPLOYMENT
            // 9. Amazon Pinpoint Project
            var pinpointApp = new Amazon.CDK.AWS.Pinpoint.CfnApp(this, "ShopFlowPinpointApp", new Amazon.CDK.AWS.Pinpoint.CfnAppProps
            {
                Name = "ShopFlowPinpointApp"
            });
            */

            // 10. Invite User Lambda (DynamoDB Stream)
            var inviteUserLambda = new Function(this, "InviteUserFunction", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Code = Amazon.CDK.AWS.Lambda.Code.FromAsset("src/Lambdas/InviteUser/publish"),
                Handler = "InviteUser::InviteUser.Function::FunctionHandler",
                Timeout = Duration.Seconds(30),
                 Environment = new Dictionary<string, string>
                {
                    { "fEMAIL_SOURCE", senderEmail.ValueAsString },
                    { "fDEVICE_TOKENS_TABLE_NAME", deviceTokensTable.TableName }
                    // { "PINPOINT_APP_ID", pinpointApp.Ref } // Disabled
                }
            });

            inviteUserLambda.AddEventSource(new DynamoEventSource(listsTable, new DynamoEventSourceProps
            {
                StartingPosition = StartingPosition.LATEST,
                BatchSize = 1,
            }));
            
            // Permissions
            listsTable.GrantStreamRead(inviteUserLambda);
            
            inviteUserLambda.AddToRolePolicy(new Amazon.CDK.AWS.IAM.PolicyStatement(new Amazon.CDK.AWS.IAM.PolicyStatementProps
            {
                Actions = new[] { "ses:SendEmail", "ses:SendRawEmail" },
                Resources = new[] { "*" } 
            }));
            
            // Allow Lambda to read Device Tokens
            deviceTokensTable.GrantReadData(inviteUserLambda);

            // 10. Grant Access to Identity Pool Role
            itemsTable.GrantReadWriteData(authenticatedRole);
            listsTable.GrantReadWriteData(authenticatedRole);
            globalProductsTable.GrantReadWriteData(authenticatedRole);
            deviceTokensTable.GrantWriteData(authenticatedRole); // AppSync writes via Resolver, but Role needs access potentially if configured in AppSync UserPoolConfig default

            // Fix: Grant access to legacy table "ShopFlow_Items" used by the App
            var legacyItemsTable = Table.FromTableName(this, "ShopFlowItemsLegacy", "ShopFlow_Items");
            legacyItemsTable.GrantReadData(authenticatedRole);
            legacyItemsTable.GrantWriteData(authenticatedRole);

           
            /*
            // Allow Lambda to Use Pinpoint
            inviteUserLambda.AddToRolePolicy(new Amazon.CDK.AWS.IAM.PolicyStatement(new Amazon.CDK.AWS.IAM.PolicyStatementProps
            {
                Actions = new[] { "mobiletargeting:SendMessages", "mobiletargeting:GetEndpoint", "mobiletargeting:UpdateEndpoint" },
                Resources = new[] { pinpointApp.AttrArn + "/*" }
            }));
            */

            // 10. Outputs
            new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = userPool.UserPoolId });
            new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = userPoolClient.UserPoolClientId });
            new CfnOutput(this, "IdentityPoolId", new CfnOutputProps { Value = identityPool.Ref });
            new CfnOutput(this, "Region", new CfnOutputProps { Value = this.Region });
            new CfnOutput(this, "ItemsTableName", new CfnOutputProps { Value = itemsTable.TableName });
            new CfnOutput(this, "ListsTableName", new CfnOutputProps { Value = listsTable.TableName });
            // 5.3. DynamoDB Table (Barcode Metrics - New)
            var metricsTable = new Table(this, "ShopFlowBarcodeMetrics", new TableProps
            {
                TableName = "ShopFlow_BarcodeMetrics",
                PartitionKey = new Attribute { Name = "barcode", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "timestamp", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

        // Product Search Lambda
        var productSearchLambda = new Function(this, "ProductSearchHandler", new FunctionProps
        {
            Runtime = Runtime.DOTNET_8,
            Code = Amazon.CDK.AWS.Lambda.Code.FromAsset("src/Lambdas/ProductSearch"),
            Handler = "ProductSearch::ProductSearch.Function::FunctionHandler",
            MemorySize = 256,
            Timeout = Duration.Seconds(10),
            LogRetention = Amazon.CDK.AWS.Logs.RetentionDays.ONE_WEEK,
            Environment = new Dictionary<string, string>
            {
                { "METRICS_TABLE_NAME", metricsTable.TableName }
            }
        });
        
        metricsTable.GrantWriteData(productSearchLambda);

        var productSearchDs = api.AddLambdaDataSource("ProductSearchDS", productSearchLambda);
        productSearchDs.CreateResolver("ProductSearchResolver", new ResolverProps
        {
            TypeName = "Query",
            FieldName = "productSearch"
        });
        
        // Output endpoint
        new CfnOutput(this, "GraphQlUrl", new CfnOutputProps { Value = api.GraphqlUrl });
        
        var smartParserApiKey = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrEmpty(smartParserApiKey))
        {
            throw new System.Exception("GEMINI_API_KEY environment variable is not set. Please set it before deploying.");
        }

        // Smart Parser Lambda (Gemini)
        var smartParserLambda = new Function(this, "SmartParserFunction", new FunctionProps
        {
            Runtime = Runtime.NODEJS_18_X,
            Code = Amazon.CDK.AWS.Lambda.Code.FromAsset("src/Lambdas/SmartParser"),
            Handler = "index.handler",
            Timeout = Duration.Seconds(30),
            Environment = new Dictionary<string, string>
            {
                { "GEMINI_API_KEY", smartParserApiKey }
            }
        });

        var smartParserDs = api.AddLambdaDataSource("SmartParserDS", smartParserLambda);
        smartParserDs.CreateResolver("ParseItemResolver", new ResolverProps
        {
            TypeName = "Query",
            FieldName = "parseItem"
        });

            // new CfnOutput(this, "PinpointAppId", new CfnOutputProps { Value = pinpointApp.Ref }); // Disabled
        }
    }
}
