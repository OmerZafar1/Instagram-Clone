using MiniInstagram.Data.Mongo.Documents;
using MongoDB.Driver;

namespace MiniInstagram.Data.Mongo;

public static class MongoIndexInitializer
{
    public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var chatMessages = database.GetCollection<ChatMessageDocument>("chat_messages");
        await chatMessages.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<ChatMessageDocument>(
                Builders<ChatMessageDocument>.IndexKeys
                    .Ascending(m => m.ConversationId)
                    .Descending(m => m.SentAt)),
            new CreateIndexModel<ChatMessageDocument>(
                Builders<ChatMessageDocument>.IndexKeys
                    .Ascending(m => m.ConversationId)
                    .Ascending(m => m.SenderId)
                    .Ascending(m => m.IsRead))
        ], cancellationToken);

        var notifications = database.GetCollection<NotificationDocument>("notifications");
        await notifications.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys
                    .Ascending(n => n.RecipientId)
                    .Descending(n => n.CreatedAt)),
            new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys
                    .Ascending(n => n.RecipientId)
                    .Ascending(n => n.IsRead))
        ], cancellationToken);
    }
}
