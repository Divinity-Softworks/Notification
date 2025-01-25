namespace DivinitySoftworks.Functions.Notification.Contracts.Requests;
/// <summary>
/// Represents a request to add an email to the blacklist.
/// </summary>
public sealed record BlackListRequest {
    /// <summary>
    /// Gets or sets the email address to be added to the blacklist.
    /// </summary>
    public required string Email { get; set; }
}
