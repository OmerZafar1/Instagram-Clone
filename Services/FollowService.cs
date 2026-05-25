using Microsoft.EntityFrameworkCore;
using MiniInstagram.Data;
using MiniInstagram.Models;

namespace MiniInstagram.Services;

public class FollowService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    INotificationPublisher notificationPublisher,
    IRateLimitService rateLimiter) : IFollowService
{
    public async Task<FollowActionResult> ToggleFollowAsync(
        string followerId,
        string followingId,
        CancellationToken cancellationToken = default)
    {
        if (followerId == followingId)
            return FollowActionResult.None;

        await rateLimiter.EnsureAllowedAsync(followerId, RateLimitPolicies.FollowUser, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var targetUser = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == followingId)
            .Select(u => new { u.Id, u.IsPrivate })
            .FirstOrDefaultAsync(cancellationToken);

        if (targetUser is null)
        {
            return FollowActionResult.None;
        }

        var existing = await db.UserFollows
            .FirstOrDefaultAsync(
                f => f.FollowerId == followerId && f.FollowingId == followingId,
                cancellationToken);

        if (existing is not null)
        {
            db.UserFollows.Remove(existing);
            await db.FollowRequests
                .Where(r => r.RequesterId == followerId && r.TargetUserId == followingId)
                .ExecuteDeleteAsync(cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return FollowActionResult.Unfollowed;
        }

        if (targetUser.IsPrivate)
        {
            var pendingRequest = await db.FollowRequests
                .FirstOrDefaultAsync(
                    r => r.RequesterId == followerId && r.TargetUserId == followingId,
                    cancellationToken);

            if (pendingRequest is not null)
            {
                db.FollowRequests.Remove(pendingRequest);
                await db.SaveChangesAsync(cancellationToken);
                return FollowActionResult.CanceledRequest;
            }

            db.FollowRequests.Add(new FollowRequest
            {
                RequesterId = followerId,
                TargetUserId = followingId,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            return FollowActionResult.Requested;
        }

        db.UserFollows.Add(new UserFollow
        {
            FollowerId = followerId,
            FollowingId = followingId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        notificationPublisher.PublishFollow(followingId, followerId);
        return FollowActionResult.Followed;
    }

    public async Task<bool> IsFollowingAsync(string followerId, string followingId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserFollows
            .AsNoTracking()
            .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId, cancellationToken);
    }

    public async Task<bool> HasPendingFollowRequestAsync(string requesterId, string targetUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.FollowRequests
            .AsNoTracking()
            .AnyAsync(r => r.RequesterId == requesterId && r.TargetUserId == targetUserId, cancellationToken);
    }

    public async Task<IReadOnlyList<FollowRequestDto>> GetPendingFollowRequestsAsync(string targetUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.FollowRequests
            .AsNoTracking()
            .Where(r => r.TargetUserId == targetUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new FollowRequestDto(
                r.Id,
                r.RequesterId,
                r.Requester.UserName ?? "",
                r.Requester.DisplayName,
                r.Requester.AvatarPath,
                r.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetFollowersAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserFollows
            .AsNoTracking()
            .Where(f => f.FollowingId == userId)
            .OrderBy(f => f.Follower.DisplayName)
            .Select(f => new UserListItemDto(
                f.FollowerId,
                f.Follower.UserName ?? "",
                f.Follower.DisplayName,
                f.Follower.AvatarPath))
            .ToListAsync(cancellationToken);
    }

    public async Task AcceptFollowRequestAsync(int requestId, string targetUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var request = await db.FollowRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.TargetUserId == targetUserId, cancellationToken);

        if (request is null)
        {
            return;
        }

        var alreadyFollowing = await db.UserFollows
            .AnyAsync(f => f.FollowerId == request.RequesterId && f.FollowingId == targetUserId, cancellationToken);

        if (!alreadyFollowing)
        {
            db.UserFollows.Add(new UserFollow
            {
                FollowerId = request.RequesterId,
                FollowingId = targetUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        db.FollowRequests.Remove(request);
        await db.SaveChangesAsync(cancellationToken);
        notificationPublisher.PublishFollow(targetUserId, request.RequesterId);
    }

    public async Task RejectFollowRequestAsync(int requestId, string targetUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var request = await db.FollowRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.TargetUserId == targetUserId, cancellationToken);

        if (request is null)
        {
            return;
        }

        db.FollowRequests.Remove(request);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetFollowerCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserFollows.AsNoTracking().CountAsync(f => f.FollowingId == userId, cancellationToken);
    }

    public async Task<int> GetFollowingCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserFollows.AsNoTracking().CountAsync(f => f.FollowerId == userId, cancellationToken);
    }

    public async Task<ApplicationUser?> GetProfileByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationUser>> SearchUsersAsync(
        string query,
        string? excludeUserId,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized))
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var usersQuery = db.Users.AsNoTracking()
            .Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(normalized)) ||
                u.DisplayName.ToLower().Contains(normalized));

        if (!string.IsNullOrEmpty(excludeUserId))
            usersQuery = usersQuery.Where(u => u.Id != excludeUserId);

        return await usersQuery
            .OrderBy(u => u.DisplayName)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
