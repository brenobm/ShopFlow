using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace InviteUser;

public class Function
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _deviceTokensTableName;

    public Function()
    {
        _sesClient = new AmazonSimpleEmailServiceClient();
        _dynamoDbClient = new AmazonDynamoDBClient();
        _deviceTokensTableName = Environment.GetEnvironmentVariable("fDEVICE_TOKENS_TABLE_NAME") ?? "ShopFlow_DeviceTokens";
        
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            try
            {
                // Try to get credentials from Env Var (JSON content) or File
                var serviceAccountJson = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_JSON");
                GoogleCredential credential;

                if (!string.IsNullOrEmpty(serviceAccountJson))
                {
                     credential = GoogleCredential.FromJson(serviceAccountJson);
                }
                else if (System.IO.File.Exists("firebase-service-account.json"))
                {
                    credential = GoogleCredential.FromFile("firebase-service-account.json");
                }
                else
                {
                     // Fallback to default (useful if deployed with GOOGLE_APPLICATION_CREDENTIALS)
                     credential = GoogleCredential.GetApplicationDefault();
                }

                FirebaseApp.Create(new AppOptions()
                {
                    Credential = credential
                });
                Console.WriteLine("Firebase App Initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Firebase: {ex.Message}");
            }
        }
    }

    public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
    {
        context.Logger.LogLine($"Beginning to process {dynamoEvent.Records.Count} records...");

        foreach (var record in dynamoEvent.Records)
        {
            if (record.EventName == "MODIFY" || record.EventName == "INSERT")
            {
                var newImage = record.Dynamodb.NewImage;
                var oldImage = record.Dynamodb.OldImage;

                List<string> newMembers = new List<string>();
                if (newImage != null && newImage.ContainsKey("members"))
                {
                     newMembers = newImage["members"].L.Select(x => x.S).ToList();
                }

                List<string> oldMembers = new List<string>();
                if (oldImage != null && oldImage.ContainsKey("members"))
                {
                     oldMembers = oldImage["members"].L.Select(x => x.S).ToList();
                }

                // Find added members
                var addedMembers = newMembers.Except(oldMembers).ToList();

                foreach (var email in addedMembers)
                {
                    context.Logger.LogLine($"Processing invite for: {email}");
                    string listName = newImage != null && newImage.ContainsKey("name") ? newImage["name"].S : "a list";
                    
                    await SendInviteEmail(email, listName);
                    await SendPushNotification(email, listName);
                }
            }
        }
    }

    private async Task SendInviteEmail(string toEmail, string listName)
    {
        var sendRequest = new SendEmailRequest
        {
            Source = Environment.GetEnvironmentVariable("fEMAIL_SOURCE") ?? "breno.machado@example.com",
            Destination = new Destination
            {
                ToAddresses = new List<string> { toEmail }
            },
            Message = new Message
            {
                Subject = new Content($"You've been invited to '{listName}' on ShopFlow!"),
                Body = new Body
                {
                    Html = new Content
                    {
                        Charset = "UTF-8",
                        Data = $@"
                        <html>
                        <body>
                            <h1>Join the shopping list!</h1>
                            <p>You have been invited to collaborate on the list <strong>{listName}</strong>.</p>
                            <p>Open the ShopFlow app to view and edit items.</p>
                            <br>
                            <p>Happy Shopping,<br>The ShopFlow Team</p>
                        </body>
                        </html>"
                    },
                    Text = new Content
                    {
                        Charset = "UTF-8",
                        Data = $"You have been invited to collaborate on the list '{listName}'. Open the ShopFlow app to view and edit items."
                    }
                }
            }
        };

        try
        {
            await _sesClient.SendEmailAsync(sendRequest);
            Console.WriteLine($"Email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email to {toEmail}: {ex.Message}");
        }
    }

    private async Task SendPushNotification(string email, string listName)
    {
        try
        {
            // Query DeviceTokens table by Email
            var request = new QueryRequest
            {
                TableName = _deviceTokensTableName,
                IndexName = "EmailIndex",
                KeyConditionExpression = "email = :v_email",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":v_email", new AttributeValue { S = email }}
                }
            };

            var response = await _dynamoDbClient.QueryAsync(request);

            if (response.Items.Count == 0)
            {
                Console.WriteLine($"No device tokens found for email: {email}");
                return;
            }

            foreach (var item in response.Items)
            {
                if (item.ContainsKey("token"))
                {
                    var token = item["token"].S;
                    await SendFCMToDevice(token, listName);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendPushNotification for {email}: {ex.Message}");
        }
    }

    private async Task SendFCMToDevice(string token, string listName)
    {
        var message = new FirebaseAdmin.Messaging.Message()
        {
            Token = token,
            Notification = new Notification()
            {
                Title = "New List Invitation",
                Body = $"You have been invited to list '{listName}'"
            },
            Data = new Dictionary<string, string>()
            {
                { "type", "invite" },
                { "listName", listName }
            }
        };

        try
        {
            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            Console.WriteLine($"Successfully sent message: {response}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending FCM message: {ex.Message}");
        }
    }
}
