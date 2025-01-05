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

/// <summary>
/// Lambda function for handling notification-related tasks such as sending emails.
/// </summary>
public sealed class Notification([FromServices] IAuthorizeService authorizeService) : ExecutableFunction(authorizeService) {
    private const string RootBase = "/notification";
    private const string RootResourceName = "DSNotification";

    /// <summary>
    /// Converts a JSON string into either an <see cref="EmailMessage"/> or an <see cref="EmailTemplateMessage"/> object.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>
    /// An <see cref="OneOf{EmailMessage, EmailTemplateMessage}"/> instance containing either an <see cref="EmailMessage"/> 
    /// or an <see cref="EmailTemplateMessage"/>, depending on the presence of the "Template" property in the JSON.
    /// </returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or cannot be deserialized into the expected types.</exception>
    private static OneOf<EmailMessage, EmailTemplateMessage> ConvertJsonToObject(string json) {
        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("Template", out _))
            return JsonSerializer.Deserialize<EmailTemplateMessage>(json)!;
        return JsonSerializer.Deserialize<EmailMessage>(json)!;
    }

    /// <summary>
    /// Lambda function to handle email notifications. Processes SNS events, parses email data, and sends emails.
    /// </summary>
    /// <param name="snsEvent">The SNS event containing the notification records.</param>
    /// <param name="context">The Lambda execution context for logging and execution details.</param>
    /// <param name="storageService">The service for loading templates from S3.</param>
    /// <param name="emailService">The service for sending emails.</param>
    /// <param name="notificationBlackListRepository">Repository for managing email blacklist entries.</param>
    [LambdaFunction(ResourceName = $"{RootResourceName}{nameof(HandleEmailAsync)}")]
    public async Task HandleEmailAsync(SNSEvent snsEvent, ILambdaContext context,
        [FromServices] IStorageService storageService,
        [FromServices] IEmailService emailService,
        [FromServices] INotificationBlackListRepository notificationBlackListRepository) {
        try {
            foreach (SNSEvent.SNSRecord? record in snsEvent.Records) {
                try {
                    context.Logger.LogInformation("Received message: {MessageId}", record.Sns.MessageId);
                    // Deserialize the SNS message to an email object
                    OneOf<EmailMessage, EmailTemplateMessage> result = ConvertJsonToObject(record.Sns.Message);

                    // Send email based on the parsed result
                    SendEmailResponse emailResult = await result.Match(
                        emailMessage => emailService.SendAsync(result.AsT0, context),
                        emailTemplateMessage => emailService.SendAsync(
                            result.AsT1,
                            (key, context) => storageService.LoadFileAsync($"templates/emails/{key}", context),
                            context)
                    );

                    context.Logger.LogInformation("Email sent successfully: {MessageId}", emailResult.MessageId);
                }
                catch (Exception exception) {
                    context.Logger.LogError(exception, "Error processing record: {Message}", exception.Message);
                }
            }
        }
        catch (Exception exception) {
            context.Logger.LogError(exception, "Unhandled error: {Message}", exception.Message);
        }
    }
}
