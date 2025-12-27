using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.DynamoDBv2.Model;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace InviteUser;

public class Function
{
    private readonly IAmazonSimpleEmailService _sesClient;

    public Function()
    {
        _sesClient = new AmazonSimpleEmailServiceClient();
    }

    public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
    {
        context.Logger.LogLine($"Beginning to process {dynamoEvent.Records.Count} records...");

        foreach (var record in dynamoEvent.Records)
        {
            context.Logger.LogLine($"Event ID: {record.EventID}");
            context.Logger.LogLine($"Event Name: {record.EventName}");

            if (record.EventName == "MODIFY")
            {
                var newImage = record.Dynamodb.NewImage;
                var oldImage = record.Dynamodb.OldImage;

                // Check if 'members' attribute exists and has changed
                if (newImage.ContainsKey("members") && oldImage.ContainsKey("members"))
                {
                    var newMembers = newImage["members"].L.Select(x => x.S).ToList();
                    var oldMembers = oldImage["members"].L.Select(x => x.S).ToList();

                    // Find added members
                    var addedMembers = newMembers.Except(oldMembers).ToList();

                    foreach (var email in addedMembers)
                    {
                        context.Logger.LogLine($"Sending invite to: {email}");
                        await SendInviteEmail(email, newImage.ContainsKey("name") ? newImage["name"].S : "a list");
                    }
                }
                // Handle case where members list was initialized (from empty to having members) vs Create (INSERT)
                // Note: logic above handles updates. INSERT usually doesn't have oldImage or members is empty.
            }
            else if (record.EventName == "INSERT")
            {
                 // Usually createList doesn't add members, but if it did:
                 var newImage = record.Dynamodb.NewImage;
                 if (newImage.ContainsKey("members"))
                 {
                     var newMembers = newImage["members"].L.Select(x => x.S).ToList();
                     foreach(var email in newMembers)
                     {
                          context.Logger.LogLine($"Sending invite to: {email}");
                          await SendInviteEmail(email, newImage.ContainsKey("name") ? newImage["name"].S : "a list");
                     }
                 }
            }
        }

        context.Logger.LogLine("Stream processing complete.");
    }

    private async Task SendInviteEmail(string toEmail, string listName)
    {
        var sendRequest = new SendEmailRequest
        {
            Source = "breno.machado@example.com", // verified identity
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
            var response = await _sesClient.SendEmailAsync(sendRequest);
            Console.WriteLine($"Email sent to {toEmail}, MessageID: {response.MessageId}");
            
            // Attempt Push Notification
            await SendPushNotification(toEmail, listName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email/push to {toEmail}: {ex.Message}");
        }
    }

    private async Task SendPushNotification(string userId, string listName)
    {
        var appId = Environment.GetEnvironmentVariable("PINPOINT_APP_ID");
        if(string.IsNullOrEmpty(appId)) return;

        try 
        {
            using var pinpoint = new Amazon.Pinpoint.AmazonPinpointClient();
            var request = new Amazon.Pinpoint.Model.SendMessagesRequest
            {
                ApplicationId = appId,
                MessageRequest = new Amazon.Pinpoint.Model.MessageRequest
                {
                    Addresses = new Dictionary<string, Amazon.Pinpoint.Model.AddressConfiguration>
                    {
                        { userId, new Amazon.Pinpoint.Model.AddressConfiguration { ChannelType = Amazon.Pinpoint.Model.ChannelType.GCM } } // Simplification: Assuming UserId maps to Endpoint or Address
                    },
                    MessageConfiguration = new Amazon.Pinpoint.Model.DirectMessageConfiguration
                    {
                        GCMMessage = new Amazon.Pinpoint.Model.GCMMessage
                        {
                            Action = Amazon.Pinpoint.Model.Action.OPEN_APP,
                            Body = $"You have been invited to list '{listName}'",
                            Title = "New List Invitation"
                        },
                        APNSMessage = new Amazon.Pinpoint.Model.APNSMessage
                        {
                            Action = Amazon.Pinpoint.Model.Action.OPEN_APP,
                            Body = $"You have been invited to list '{listName}'",
                            Title = "New List Invitation"
                        }
                    }
                }
            };
            
            // Note: This relies on the 'userId' being a valid Endpoint ID or Address in Pinpoint.
            // Since we haven't implemented the registration logic fully, this serves as the backend scaffolding.
            await pinpoint.SendMessagesAsync(request);
            Console.WriteLine($"Push sent to {userId}");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Push failed: {ex.Message}");
        }
    }
}
