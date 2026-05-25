using MiniInstagram.Models;

namespace MiniInstagram.Services;

public interface INotificationService
{
    Task<NotificationDto> CreateAsync(
        string recipientId,
        string actorId,
        NotificationType type,
        string message,
        int? postId = null,
        int? conversationId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<NotificationDto>> GetForUserAsync(string userId, int take = 50, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);

    Task MarkAsReadAsync(int notificationId, string userId, CancellationToken ct = default);

    Task MarkAllAsReadAsync(string userId, CancellationToken ct = default);
}
