using MiniInstagram.Data.Mongo.Documents;
using MongoDB.Driver;

namespace MiniInstagram.Services.Mongo;

public class NotificationStore(IMongoDatabase database) : INotificationStore
{
    private readonly IMongoCollection<NotificationDocument> _notifications =
        database.GetCollection<NotificationDocument>("notifications");

    public async Task<NotificationDocument> InsertAsync(
        NotificationDocument notification,
        CancellationToken cancellationToken = default)
    {
        await _notifications.InsertOneAsync(notification, cancellationToken: cancellationToken);
        return notification;
    }

    public async Task<IReadOnlyList<NotificationDocument>> GetForUserAsync(
        string userId,
        int take,
        CancellationToken cancellationToken = default) =>
        await _notifications.Find(n => n.RecipientId == userId)
            .SortByDescending(n => n.CreatedAt)
            .Limit(take)
            .ToListAsync(cancellationToken);

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default) =>
        (int)await _notifications.CountDocumentsAsync(
            n => n.RecipientId == userId && !n.IsRead,
            cancellationToken: cancellationToken);

    public async Task<bool> MarkAsReadAsync(
        string notificationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationDocument>.Filter.And(
            Builders<NotificationDocument>.Filter.Eq(n => n.Id, notificationId),
            Builders<NotificationDocument>.Filter.Eq(n => n.RecipientId, userId));

        var update = Builders<NotificationDocument>.Update.Set(n => n.IsRead, true);
        var result = await _notifications.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationDocument>.Filter.And(
            Builders<NotificationDocument>.Filter.Eq(n => n.RecipientId, userId),
            Builders<NotificationDocument>.Filter.Eq(n => n.IsRead, false));

        var update = Builders<NotificationDocument>.Update.Set(n => n.IsRead, true);
        await _notifications.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }
}
