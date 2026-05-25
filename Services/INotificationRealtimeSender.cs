using MiniInstagram.Models;

namespace MiniInstagram.Services;

public interface INotificationRealtimeSender
{
    Task SendToUserAsync(string recipientId, NotificationDto notification, CancellationToken ct = default);
    Task SendUnreadCountAsync(string recipientId, int count, CancellationToken ct = default);
}
