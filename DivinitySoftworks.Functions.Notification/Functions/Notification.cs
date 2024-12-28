using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using DivinitySoftworks.AWS.Core.Web.Functions;
using DivinitySoftworks.Core.Web.Security;
using DivinitySoftworks.Functions.Notification.Repositories;
using System.Text.Json;

namespace DS.Functions.Notification;

public sealed class Notification([FromServices] IAuthorizeService authorizeService) : ExecutableFunction(authorizeService) {
    const string RootBase = "/notification";
    const string RootResourceName = "DSNotification";

    [LambdaFunction(ResourceName = $"{RootResourceName}{nameof(HandleEmailAsync)}")]
    public async Task HandleEmailAsync(SNSEvent snsEvent, ILambdaContext context,
        [FromServices] IAmazonSimpleEmailService simpleEmailService,
        [FromServices] INotificationBlackListRepository mortgageInterestRepository) {
        try {
            foreach (var record in snsEvent.Records) {
                try {
                    var message = JsonSerializer.Deserialize<EmailMessage>(record.Sns.Message);

                    var sendRequest = new SendEmailRequest {
                        Source = senderAddress,
                        Destination = new Destination {
                            ToAddresses =
                        new List<string> { message.Recipient }
                        },
                        Message = new Message {
                            Subject = new Content(message.Subject),
                            Body = new Body {
                                Html = new Content {
                                    Charset = "UTF-8",
                                    Data = message.Body
                                },
                                Text = new Content {
                                    Charset = "UTF-8",
                                    Data = message.Body
                                }
                            }
                        },
                        // If you are not using a configuration set, comment
                        // or remove the following line 
                        //ConfigurationSetName = configSet
                    };
                    try {
                        context.Logger.LogWarning("Sending email using Amazon SES...");
                        var response = await simpleEmailService.SendEmailAsync(sendRequest);
                        context.Logger.LogWarning("The email was sent successfully.");
                    }
                    catch (Exception ex) {

                        context.Logger.LogError("The email was not sent.");
                        context.Logger.LogError(ex.Message);

                    }
                }
                catch (Exception ex) {
                    context.Logger.LogLine($"Error sending email: {ex.Message}");
                }
            }
        }
        catch (Exception exception) {
            context.Logger.LogError(exception.Message);
        }
    }

    static readonly string senderAddress = "mortgage@divinity-softworks.com";
}

public class EmailMessage {
    public string Subject { get; set; }
    public string Body { get; set; }
    public string Recipient { get; set; }
}
