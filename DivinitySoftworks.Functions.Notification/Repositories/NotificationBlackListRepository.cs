using Amazon.DynamoDBv2;
using DivinitySoftworks.Functions.Notification.Data;

namespace DivinitySoftworks.Functions.Notification.Repositories; 

/// <summary>
/// Interface for managing the notification blacklist in a DynamoDB table.
/// </summary>
public interface INotificationBlackListRepository {
    /// <summary>
    /// Adds a new blacklist item to the DynamoDB table.
    /// </summary>
    /// <param name="blackListItem">The blacklist item to create.</param>
    /// <returns>A boolean indicating whether the operation was successful.</returns>
    Task<bool> CreateAsync(BlackListItem blackListItem);

    /// <summary>
    /// Retrieves a blacklist item by its primary key (email).
    /// </summary>
    /// <param name="pk">The primary key (email) of the blacklist item.</param>
    /// <returns>The blacklist item if found; otherwise, null.</returns>
    Task<BlackListItem?> ReadAsync(string pk);

    /// <summary>
    /// Deletes a blacklist item from the DynamoDB table by its primary key (email).
    /// </summary>
    /// <param name="pk">The primary key (email) of the blacklist item to delete.</param>
    /// <returns>A boolean indicating whether the operation was successful.</returns>
    Task<bool> DeleteAsync(string pk);
}

/// <summary>
/// Implementation of <see cref="INotificationBlackListRepository"/> using DynamoDB as the storage backend.
/// </summary>
public sealed class NotificationBlackListRepository(IAmazonDynamoDB amazonDynamoDB) : INotificationBlackListRepository {
    private readonly string _tableName = "Notification.BlackList";
    private readonly IAmazonDynamoDB _amazonDynamoDB = amazonDynamoDB;

    /// <inheritdoc />
    public Task<bool> CreateAsync(BlackListItem blackListItem) {
        return _amazonDynamoDB.CreateItemAsync(_tableName, blackListItem);
    }

    /// <inheritdoc />
    public Task<BlackListItem?> ReadAsync(string pk) {
        return _amazonDynamoDB.GetItemAsync<BlackListItem?>(
            _tableName,
            pk.ToLower()
        );
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string pk) {
        return _amazonDynamoDB.DeleteItemAsync(_tableName, pk.ToLower());
    }
}
