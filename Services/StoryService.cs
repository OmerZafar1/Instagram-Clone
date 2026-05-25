using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using MiniInstagram.Data;
using MiniInstagram.Hubs;
using MiniInstagram.Models;

namespace MiniInstagram.Services;

public class StoryService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IImageStorageService mediaStorage,
    IChatService chatService,
    IHubContext<ChatHub> chatHub,
    IRateLimitService rateLimiter) : IStoryService
{
    private static readonly TimeSpan StoryLifetime = TimeSpan.FromHours(1);
    private static readonly HashSet<string> AllowedReactions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Love", "Laugh", "Fire", "Clap", "Wow"
    };

    public async Task<Story> CreateStoryAsync(
        string userId,
        string? caption,
        Stream mediaStream,
        string fileName,
        string mediaType,
        CancellationToken cancellationToken = default)
    {
        var mediaPath = await mediaStorage.SaveStoryMediaAsync(mediaStream, fileName, cancellationToken);
        var now = DateTime.UtcNow;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var story = new Story
        {
            UserId = userId,
            Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim(),
            MediaPath = mediaPath,
            MediaType = NormalizeMediaType(mediaType),
            CreatedAt = now,
            ExpiresAt = now.Add(StoryLifetime)
        };

        db.Stories.Add(story);
        await db.SaveChangesAsync(cancellationToken);
        return story;
    }

    public async Task<IReadOnlyList<StoryTrayItem>> GetStoryTrayAsync(
        string currentUserId,
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var visibleUserIds = await GetFollowingIdsAsync(db, currentUserId, cancellationToken);
        visibleUserIds.Add(currentUserId);

        var now = DateTime.UtcNow;
        var activeStories = await db.Stories
            .AsNoTracking()
            .Where(s => s.ExpiresAt > now && visibleUserIds.Contains(s.UserId))
            .Select(s => new
            {
                s.UserId,
                UserName = s.User.UserName ?? "",
                s.User.DisplayName,
                s.User.AvatarPath,
                s.CreatedAt,
                IsViewed = s.UserId == currentUserId || s.Views.Any(v => v.ViewerId == currentUserId)
            })
            .ToListAsync(cancellationToken);

        return activeStories
            .GroupBy(s => new { s.UserId, s.UserName, s.DisplayName, s.AvatarPath })
            .Select(g => new StoryTrayItem(
                g.Key.UserId,
                g.Key.UserName,
                g.Key.DisplayName,
                g.Key.AvatarPath,
                g.Max(s => s.CreatedAt),
                g.Count(),
                g.Count(s => !s.IsViewed),
                g.Key.UserId == currentUserId))
            .OrderByDescending(s => s.IsCurrentUser)
            .ThenByDescending(s => s.UnviewedCount > 0)
            .ThenByDescending(s => s.LatestStoryAt)
            .Take(take)
            .ToList();
    }

    public async Task<IReadOnlyList<StoryViewItem>> GetVisibleStoriesByUserAsync(
        string ownerId,
        string viewerId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await CanViewStoriesAsync(db, ownerId, viewerId, cancellationToken))
        {
            return [];
        }

        var now = DateTime.UtcNow;
        return await db.Stories
            .AsNoTracking()
            .Where(s => s.UserId == ownerId && s.ExpiresAt > now)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new StoryViewItem(
                s.Id,
                s.UserId,
                s.User.UserName ?? "",
                s.User.DisplayName,
                s.User.AvatarPath,
                s.MediaPath,
                s.MediaType,
                s.Caption,
                s.CreatedAt,
                s.ExpiresAt,
                s.Views.Count,
                s.UserId == viewerId || s.Views.Any(v => v.ViewerId == viewerId)))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasVisibleActiveStoryAsync(
        string ownerId,
        string viewerId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await CanViewStoriesAsync(db, ownerId, viewerId, cancellationToken))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        return await db.Stories
            .AsNoTracking()
            .AnyAsync(s => s.UserId == ownerId && s.ExpiresAt > now, cancellationToken);
    }

    public async Task<bool> MarkViewedAsync(
        int storyId,
        string viewerId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var story = await db.Stories
            .AsNoTracking()
            .Where(s => s.Id == storyId)
            .Select(s => new { s.Id, s.UserId, s.ExpiresAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (story is null || story.ExpiresAt <= DateTime.UtcNow || story.UserId == viewerId)
        {
            return false;
        }

        if (!await CanViewStoriesAsync(db, story.UserId, viewerId, cancellationToken))
        {
            return false;
        }

        var alreadyViewed = await db.StoryViews
            .AnyAsync(v => v.StoryId == storyId && v.ViewerId == viewerId, cancellationToken);

        if (alreadyViewed)
        {
            return false;
        }

        db.StoryViews.Add(new StoryView
        {
            StoryId = storyId,
            ViewerId = viewerId,
            ViewedAt = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<StoryViewerItem>> GetStoryViewersAsync(
        int storyId,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var storyOwnerId = await db.Stories
            .AsNoTracking()
            .Where(s => s.Id == storyId)
            .Select(s => s.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (storyOwnerId != ownerId)
        {
            return [];
        }

        return await db.StoryViews
            .AsNoTracking()
            .Where(v => v.StoryId == storyId)
            .OrderByDescending(v => v.ViewedAt)
            .Select(v => new StoryViewerItem(
                v.ViewerId,
                v.Viewer.UserName ?? "",
                v.Viewer.DisplayName,
                v.Viewer.AvatarPath,
                v.ViewedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatMessageDto?> ReactToStoryAsync(
        int storyId,
        string viewerId,
        string reaction,
        CancellationToken cancellationToken = default)
    {
        await rateLimiter.EnsureAllowedAsync(viewerId, RateLimitPolicies.StoryReaction, cancellationToken);

        var normalizedReaction = NormalizeReaction(reaction);
        var message = $"Reacted {normalizedReaction} to your story.";

        return await SendStoryMessageAsync(storyId, viewerId, message, cancellationToken);
    }

    public async Task<ChatMessageDto?> ReplyToStoryAsync(
        int storyId,
        string viewerId,
        string reply,
        CancellationToken cancellationToken = default)
    {
        var trimmed = reply.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ArgumentException("Reply cannot be empty.", nameof(reply));
        }

        await rateLimiter.EnsureAllowedAsync(viewerId, RateLimitPolicies.StoryReply, cancellationToken);

        var message = $"Replied to your story: {trimmed}";
        return await SendStoryMessageAsync(storyId, viewerId, message, cancellationToken);
    }

    public async Task<int> DeleteExpiredStoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var expiredStories = await db.Stories
            .Where(s => s.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expiredStories.Count == 0)
        {
            return 0;
        }

        db.Stories.RemoveRange(expiredStories);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var story in expiredStories)
        {
            mediaStorage.DeleteIfExists(story.MediaPath);
        }

        return expiredStories.Count;
    }

    private async Task<ChatMessageDto?> SendStoryMessageAsync(
        int storyId,
        string viewerId,
        string message,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var story = await db.Stories
            .AsNoTracking()
            .Where(s => s.Id == storyId)
            .Select(s => new { s.Id, s.UserId, s.ExpiresAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (story is null || story.ExpiresAt <= DateTime.UtcNow || story.UserId == viewerId)
        {
            return null;
        }

        if (!await CanViewStoriesAsync(db, story.UserId, viewerId, cancellationToken))
        {
            return null;
        }

        var conversation = await chatService.GetOrCreateConversationAsync(
            viewerId,
            story.UserId,
            cancellationToken);

        var dto = await chatService.SendMessageAsync(
            conversation.Id,
            viewerId,
            message,
            cancellationToken);

        await chatHub.Clients
            .Group(ChatHub.GetGroupName(conversation.Id))
            .SendAsync("ReceiveMessage", dto, cancellationToken);

        return dto;
    }

    private static async Task<HashSet<string>> GetFollowingIdsAsync(
        ApplicationDbContext db,
        string userId,
        CancellationToken cancellationToken)
    {
        return await db.UserFollows
            .AsNoTracking()
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToHashSetAsync(cancellationToken);
    }

    private static async Task<bool> CanViewStoriesAsync(
        ApplicationDbContext db,
        string ownerId,
        string viewerId,
        CancellationToken cancellationToken)
    {
        if (ownerId == viewerId)
        {
            return true;
        }

        return await db.UserFollows
            .AsNoTracking()
            .AnyAsync(f => f.FollowerId == viewerId && f.FollowingId == ownerId, cancellationToken);
    }

    private static string NormalizeMediaType(string mediaType) =>
        mediaType.StartsWith("video", StringComparison.OrdinalIgnoreCase) ? "video" : "image";

    private static string NormalizeReaction(string reaction)
    {
        var trimmed = reaction.Trim();
        if (AllowedReactions.Contains(trimmed))
        {
            return trimmed;
        }

        throw new ArgumentException("Reaction is not supported.", nameof(reaction));
    }
}
