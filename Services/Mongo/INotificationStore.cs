using MiniInstagram.Data.Mongo.Documents;

namespace MiniInstagram.Services.Mongo;

public interface INotificationStore
{
    Task<NotificationDocument> InsertAsync(NotificationDocument notification, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDocument>> GetForUserAsync(string userId, int take, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(string notificationId, string userId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
}
