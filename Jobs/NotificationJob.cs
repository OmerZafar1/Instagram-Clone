using MiniInstagram.Models;
using MiniInstagram.Services;

namespace MiniInstagram.Jobs;

/// <summary>
/// Hangfire background jobs: persist notification to MongoDB, then push via SignalR.
/// </summary>
public class NotificationJob(
    INotificationService notifications,
    INotificationRealtimeSender realtime)
{
    public async Task SendLikeAsync(string recipientId, string actorId, int postId)
    {
        if (recipientId == actorId) return;

        var dto = await notifications.CreateAsync(
            recipientId, actorId, NotificationType.Like,
            "liked your post.", postId);

        await PushAsync(recipientId, dto);
    }

    public async Task SendCommentAsync(string recipientId, string actorId, int postId)
    {
        if (recipientId == actorId) return;

        var dto = await notifications.CreateAsync(
            recipientId, actorId, NotificationType.Comment,
            "commented on your post.", postId);

        await PushAsync(recipientId, dto);
    }

    public async Task SendFollowAsync(string recipientId, string actorId)
    {
        if (recipientId == actorId) return;

        var dto = await notifications.CreateAsync(
            recipientId, actorId, NotificationType.Follow,
            "started following you.");

        await PushAsync(recipientId, dto);
    }

    public async Task SendMessageAsync(string recipientId, string actorId, int conversationId, string preview)
    {
        if (recipientId == actorId) return;

        var text = string.IsNullOrWhiteSpace(preview) ? "sent you a message." : $"sent: {preview}";
        var dto = await notifications.CreateAsync(
            recipientId, actorId, NotificationType.Message,
            text, conversationId: conversationId);

        await PushAsync(recipientId, dto);
    }

    private async Task PushAsync(string recipientId, NotificationDto dto)
    {
        await realtime.SendToUserAsync(recipientId, dto);
        var count = await notifications.GetUnreadCountAsync(recipientId);
        await realtime.SendUnreadCountAsync(recipientId, count);
    }
}
