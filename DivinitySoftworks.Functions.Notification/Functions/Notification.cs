using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.SimpleEmail.Model;
using DivinitySoftworks.AWS.Core.Net.Mail;
using DivinitySoftworks.AWS.Core.Net.Storage;
using DivinitySoftworks.AWS.Core.Web.Functions;
using DivinitySoftworks.Core.Net.Mail;
using DivinitySoftworks.Core.Web.Security;
using DivinitySoftworks.Functions.Notification.Repositories;
using OneOf;
using System.Text.Json;

namespace DS.Functions.Notification;

public sealed class Notification([FromServices] IAuthorizeService authorizeService) : ExecutableFunction(authorizeService) {
    const string RootBase = "/notification";
    const string RootResourceName = "DSNotification";

    /// <summary>
    /// Converts a JSON string into either an <see cref="EmailMessage"/> or an <see cref="EmailTemplateMessage"/> object.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>
    /// An <see cref="OneOf{EmailMessage, EmailTemplateMessage}"/> instance containing either an <see cref="EmailMessage"/> 
    /// or an <see cref="EmailTemplateMessage"/> depending on the presence of the "Template" property in the JSON.
    /// </returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or cannot be deserialized into the expected types.</exception>
    private static OneOf<EmailMessage, EmailTemplateMessage> ConvertJsonToObject(string json) {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("Template", out _))
            return JsonSerializer.Deserialize<EmailTemplateMessage>(json)!;
        return JsonSerializer.Deserialize<EmailMessage>(json)!;
    }

    [LambdaFunction(ResourceName = $"{RootResourceName}{nameof(HandleEmailAsync)}")]
    public async Task HandleEmailAsync(SNSEvent snsEvent, ILambdaContext context,
        [FromServices] IStorageService storageService,
        [FromServices] IEmailService emailService,
        [FromServices] INotificationBlackListRepository notificationBlackListRepository) {
        try {
            foreach (SNSEvent.SNSRecord? record in snsEvent.Records) {
                try {

                    context.Logger.LogLine($"Received message: {record.Sns.Message}");

                    OneOf<EmailMessage, EmailTemplateMessage> result = ConvertJsonToObject(record.Sns.Message);

                    SendEmailResponse emailResultTask = await result.Match(
                        EmailMessage => emailService.SendAsync(result.AsT0, context),
                        EmailTemplateMessage => emailService.SendAsync(result.AsT1
                        , storageService.LoadFileAsync
                        , context)
                    );
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
}
