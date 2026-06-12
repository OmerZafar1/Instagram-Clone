using Microsoft.EntityFrameworkCore;
using MiniInstagram.Data;
using MiniInstagram.Data.Mongo.Documents;
using MiniInstagram.Models;
using MiniInstagram.Services.Mongo;

namespace MiniInstagram.Services;

public class NotificationService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    INotificationStore notifications) : INotificationService
{
    public async Task<NotificationDto> CreateAsync(
        string recipientId,
        string actorId,
        NotificationType type,
        string message,
        int? postId = null,
        int? conversationId = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var actor = await db.Users.AsNoTracking()
            .FirstAsync(u => u.Id == actorId, ct);

        var notification = new NotificationDocument
        {
            RecipientId = recipientId,
            ActorId = actorId,
            Type = type,
            Message = message,
            PostId = postId,
            ConversationId = conversationId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await notifications.InsertAsync(notification, ct);
        return ToDto(notification, actor);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(
        string userId,
        int take = 50,
        CancellationToken ct = default)
    {
        var items = await notifications.GetForUserAsync(userId, take, ct);
        if (items.Count == 0)
        {
            return [];
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var actorIds = items.Select(n => n.ActorId).Distinct().ToList();
        var actors = await db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        return items
            .Select(n => ToDto(n, actors.GetValueOrDefault(n.ActorId)))
            .ToList();
    }

    public Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default) =>
        notifications.GetUnreadCountAsync(userId, ct);

    public Task MarkAsReadAsync(string notificationId, string userId, CancellationToken ct = default) =>
        notifications.MarkAsReadAsync(notificationId, userId, ct);

    public Task MarkAllAsReadAsync(string userId, CancellationToken ct = default) =>
        notifications.MarkAllAsReadAsync(userId, ct);

    private static NotificationDto ToDto(NotificationDocument n, ApplicationUser? actor) =>
        new(
            n.Id ?? "",
            n.ActorId,
            actor?.UserName ?? "",
            actor?.DisplayName ?? "Unknown",
            actor?.AvatarPath,
            n.Type,
            n.Message,
            n.PostId,
            n.ConversationId,
            n.IsRead,
            n.CreatedAt);
}
