using Amazon.DynamoDBv2;

namespace DivinitySoftworks.Functions.Notification.Repositories {

    public interface INotificationBlackListRepository { }

    public sealed class NotificationBlackListRepository(IAmazonDynamoDB amazonDynamoDB) : INotificationBlackListRepository {
        readonly string _tableName = "Notification.BlackList";
        readonly IAmazonDynamoDB _amazonDynamoDB = amazonDynamoDB;
    }
}
