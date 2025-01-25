using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.SimpleEmail.Model;
using DivinitySoftworks.AWS.Core.Net.Mail;
using DivinitySoftworks.AWS.Core.Net.Storage;
using DivinitySoftworks.AWS.Core.Web.Functions;
using DivinitySoftworks.Core.Net.Mail;
using DivinitySoftworks.Core.Web.Errors;
using DivinitySoftworks.Core.Web.Security;
using DivinitySoftworks.Functions.Notification.Contracts.Requests;
using DivinitySoftworks.Functions.Notification.Contracts.Responses;
using DivinitySoftworks.Functions.Notification.Data;
using DivinitySoftworks.Functions.Notification.Repositories;
using OneOf;
using System;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using static Amazon.Lambda.Annotations.APIGateway.HttpResults;

namespace DS.Functions.Notification;

/// <summary>
/// Lambda function for handling notification-related tasks such as sending emails.
/// </summary>
public sealed class Notification([FromServices] IAuthorizeService authorizeService) : ExecutableFunction(authorizeService) {
    private const string RootBase = "/notification";
    private const string RootResourceName = "DSNotification";

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
                        emailMessage => emailService.SendAsync(result.AsT0, null, context),
                        emailTemplateMessage => emailService.SendAsync(
                            result.AsT1,
                            (key, context) => storageService.LoadFileAsync($"templates/emails/{key}", context),
                            async (key, context) => await FilterBlacklistedEmailsAsync(key, notificationBlackListRepository, context),
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

    /// <summary>
    /// Handles an HTTP POST request to add a valid email address to the notification blacklist.
    /// </summary>
    /// <param name="context">The AWS Lambda context providing runtime information.</param>
    /// <param name="request">The incoming API Gateway HTTP request.</param>
    /// <param name="blackListRequest">The request payload containing the email address to be blacklisted.</param>
    /// <param name="notificationBlackListRepository">The repository interface for interacting with the notification blacklist storage.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is an HTTP response:
    /// <list type="bullet">
    /// <item><description>Returns <c>400 Bad Request</c> if the email address is invalid.</description></item>
    /// <item><description>Returns <c>201 Created</c> if the email address was successfully added to the blacklist.</description></item>
    /// </list>
    /// </returns>
    [LambdaFunction(ResourceName = $"{RootResourceName}{nameof(PostAsync)}")]
    [HttpApi(LambdaHttpMethod.Post, $"{RootBase}")]
    public async Task<IHttpResult> PostAsync(
        ILambdaContext context,
        APIGatewayHttpApiV2ProxyRequest request,
        [FromBody] BlackListRequest blackListRequest,
        [FromServices] INotificationBlackListRepository notificationBlackListRepository) {

        return await ExecuteAsync(Authorize.AllowAnonymous, context, request, async () => {
            try {
                if (!MailAddress.TryCreate(blackListRequest.Email, out MailAddress? mailAddress) || mailAddress is null)
                    return BadRequest(new ErrorResponse(HttpStatusCode.BadRequest, "invalid_request", "The 'email' is not a valid email address."));

                blackListRequest.Email = mailAddress.Address.ToLower();

                await notificationBlackListRepository.CreateAsync(new BlackListItem {
                    Email = blackListRequest.Email,
                    Date = DateTime.UtcNow.ToUnixTimeSeconds()
                });
            }
            catch (Exception exception) {
                context.Logger.LogError(exception, "Unable to save the email adress: {Email}.", blackListRequest.Email);
            }

            //// Return a Created (201) response with the created black list data.
            return Created(null, new BlackListResponse() { 
                IsSuccessful = true,
                Email = blackListRequest.Email
            });
        });
    }

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
    /// Filters a list of email addresses by checking each one against a blacklist repository.
    /// Only non-blacklisted and valid email addresses are returned.
    /// </summary>
    /// <param name="emailAdresses">A list of email addresses to be checked.</param>
    /// <param name="notificationBlackListRepository">The repository used to check if an email is blacklisted.</param>
    /// <param name="context">The Lambda context, used for logging and other AWS Lambda-related operations.</param>
    /// <returns>A list of valid email addresses that are not blacklisted.</returns>
    /// <remarks>
    /// This method uses the <see cref="MailAddress.TryCreate(string, out MailAddress)"/> method to validate the format of each email.
    /// Emails that fail format validation or are found in the blacklist are excluded from the result.
    /// </remarks>
    private static async Task<List<string>> FilterBlacklistedEmailsAsync(List<string> emailAdresses, INotificationBlackListRepository notificationBlackListRepository, ILambdaContext context) {
        if (emailAdresses.Count == 0) return [];

        List<string> validEmails = [];

        foreach (string email in emailAdresses) {
            if (!MailAddress.TryCreate(email, out var emailAdress)) {
                context.Logger.LogError("An invalid email was provided: {Email}", email);
                continue;
            }
            if ((await notificationBlackListRepository.ReadAsync(emailAdress.Address)) is not null)
                continue;
            validEmails.Add(email);
        }

        return validEmails;
    }
}
