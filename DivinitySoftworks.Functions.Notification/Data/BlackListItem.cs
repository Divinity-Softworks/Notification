using System.Text.Json.Serialization;

namespace DivinitySoftworks.Functions.Notification.Data;

/// <summary>
/// Represents a blacklist item, containing an email address and the associated date.
/// </summary>
public sealed record BlackListItem {

    /// <summary>
    /// Gets the primary key, which is derived from the email address.
    /// </summary>
    [JsonIgnore]
    public string PK => Email;

    /// <summary>
    /// Gets or sets the email address of the blacklisted item.
    /// </summary>
    [JsonPropertyName("Email")]
    public string Email { get; init; } = default!;

    /// <summary>
    /// Gets the timestamp representing the date the email was blacklisted.
    /// </summary>
    [JsonPropertyName("Date")]
    public long Date { get; init; } = default!;

    /// <summary>
    /// Gets the date and time in UTC derived from the Unix timestamp.
    /// </summary>
    [JsonIgnore]
    public DateTime DateTime => Date.FromUnixTimeSeconds();
}
