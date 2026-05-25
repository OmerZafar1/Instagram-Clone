using Microsoft.EntityFrameworkCore;
using MiniInstagram.Data;
using MiniInstagram.Models;

namespace MiniInstagram.Services;

public class NotificationService(IDbContextFactory<ApplicationDbContext> dbFactory) : INotificationService
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

        var notification = new Notification
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

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        return ToDto(notification, actor);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(
        string userId,
        int take = 50,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var items = await db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == userId)
            .Include(n => n.Actor)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

        return items.Select(n => ToDto(n, n.Actor)).ToList();
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientId == userId && !n.IsRead, ct);
    }

    public async Task MarkAsReadAsync(int notificationId, string userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId, ct);
        if (notification is null) return;

        notification.IsRead = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    private static NotificationDto ToDto(Notification n, Data.ApplicationUser actor) =>
        new(
            n.Id,
            n.ActorId,
            actor.UserName ?? "",
            actor.DisplayName,
            actor.AvatarPath,
            n.Type,
            n.Message,
            n.PostId,
            n.ConversationId,
            n.IsRead,
            n.CreatedAt);
}
