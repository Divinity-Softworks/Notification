using System.Text.Json.Serialization;

namespace DivinitySoftworks.Functions.Notification.Contracts.Responses;
/// <summary>
/// Represents a response for a blacklist request.
/// </summary>
internal sealed record BlackListResponse {
    /// <summary>
    /// Gets or sets a value indicating whether the request to blacklist an email was successful.
    /// </summary>
    [JsonPropertyName("is_successful")]
    public required bool IsSuccessful { get; init; }
    /// <summary>
    /// Gets or sets the email address that was processed in the blacklist operation.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }
}
