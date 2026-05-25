using Microsoft.EntityFrameworkCore;
using MiniInstagram.Data;
using MiniInstagram.Models;

namespace MiniInstagram.Services;

public class PostService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IImageStorageService imageStorage,
    INotificationPublisher notificationPublisher,
    IRateLimitService rateLimiter) : IPostService
{
    private static readonly TimeSpan CommentEditWindow = TimeSpan.FromMinutes(30);

    public async Task<Post> CreatePostAsync(
        string userId,
        string caption,
        Stream mediaStream,
        string fileName,
        string mediaType,
        PostVisibility visibility = PostVisibility.Public,
        IReadOnlyCollection<string>? selectedViewerIds = null,
        CancellationToken cancellationToken = default)
    {
        await rateLimiter.EnsureAllowedAsync(userId, RateLimitPolicies.CreatePost, cancellationToken);

        var mediaPath = await imageStorage.SavePostMediaAsync(mediaStream, fileName, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var normalizedVisibility = visibility;
        var selectedIds = selectedViewerIds?
            .Where(id => !string.IsNullOrWhiteSpace(id) && id != userId)
            .Distinct()
            .ToList() ?? [];

        if (normalizedVisibility == PostVisibility.SelectedFollowers && selectedIds.Count == 0)
        {
            normalizedVisibility = PostVisibility.FollowersOnly;
        }

        var post = new Post
        {
            UserId = userId,
            Caption = caption.Trim(),
            ImagePath = mediaPath,
            MediaType = NormalizeMediaType(mediaType, fileName),
            Visibility = normalizedVisibility,
            CreatedAt = DateTime.UtcNow
        };

        db.Posts.Add(post);
        await db.SaveChangesAsync(cancellationToken);

        if (normalizedVisibility == PostVisibility.SelectedFollowers)
        {
            var approvedFollowerIds = await db.UserFollows
                .AsNoTracking()
                .Where(f => f.FollowingId == userId && selectedIds.Contains(f.FollowerId))
                .Select(f => f.FollowerId)
                .ToListAsync(cancellationToken);

            db.PostVisibleUsers.AddRange(approvedFollowerIds.Select(viewerId => new PostVisibleUser
            {
                PostId = post.Id,
                UserId = viewerId
            }));
            await db.SaveChangesAsync(cancellationToken);
        }

        return post;
    }

    public async Task<IReadOnlyList<Post>> GetFeedAsync(string userId, int take = 20, CancellationToken cancellationToken = default)
    {
        var page = await GetFeedPageAsync(userId, take: take, cancellationToken: cancellationToken);
        return page.Items;
    }

    public async Task<FeedPageDto> GetFeedPageAsync(
        string userId,
        DateTime? beforeCreatedAt = null,
        int? beforeId = null,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var pageSize = Math.Clamp(take, 1, 50);
        var visiblePrivateUserIds = await GetFollowingIdsAsync(db, userId, cancellationToken);
        IQueryable<Post> query = db.Posts
            .AsNoTracking()
            .Where(p =>
                (!p.User.IsPrivate || p.UserId == userId || visiblePrivateUserIds.Contains(p.UserId)) &&
                (p.Visibility == PostVisibility.Public ||
                    p.UserId == userId ||
                    (p.Visibility == PostVisibility.FollowersOnly && visiblePrivateUserIds.Contains(p.UserId)) ||
                    (p.Visibility == PostVisibility.SelectedFollowers && p.VisibleUsers.Any(v => v.UserId == userId))))
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.SavedByUsers)
            .Include(p => p.VisibleUsers)
            .Include(p => p.Comments).ThenInclude(c => c.User);

        if (beforeCreatedAt is not null && beforeId is not null)
        {
            query = query.Where(p =>
                p.CreatedAt < beforeCreatedAt.Value ||
                (p.CreatedAt == beforeCreatedAt.Value && p.Id < beforeId.Value));
        }

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var nextCursor = items.LastOrDefault();
        return new FeedPageDto(
            items,
            nextCursor?.CreatedAt,
            nextCursor?.Id,
            hasMore);
    }

    public async Task<IReadOnlyList<Post>> GetUserPostsAsync(string userId, int take = 50, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Posts
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.SavedByUsers)
            .Include(p => p.VisibleUsers)
            .Include(p => p.Comments).ThenInclude(c => c.User)
            .OrderByDescending(p => p.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetUserPostsForViewerAsync(string userId, string? viewerId, int take = 50, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        List<string> visiblePrivateUserIds = string.IsNullOrEmpty(viewerId)
            ? []
            : await GetFollowingIdsAsync(db, viewerId, cancellationToken);

        return await db.Posts
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Where(p =>
                (!p.User.IsPrivate || p.UserId == viewerId || visiblePrivateUserIds.Contains(p.UserId)) &&
                (p.Visibility == PostVisibility.Public ||
                    p.UserId == viewerId ||
                    (p.Visibility == PostVisibility.FollowersOnly && visiblePrivateUserIds.Contains(p.UserId)) ||
                    (p.Visibility == PostVisibility.SelectedFollowers && p.VisibleUsers.Any(v => v.UserId == viewerId))))
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.SavedByUsers)
            .Include(p => p.VisibleUsers)
            .Include(p => p.Comments).ThenInclude(c => c.User)
            .OrderByDescending(p => p.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUserPostCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Posts.AsNoTracking().CountAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetSavedPostsAsync(string userId, int take = 50, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var visiblePrivateUserIds = await GetFollowingIdsAsync(db, userId, cancellationToken);

        return await db.SavedPosts
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Where(s =>
                (!s.Post.User.IsPrivate || s.Post.UserId == userId || visiblePrivateUserIds.Contains(s.Post.UserId)) &&
                (s.Post.Visibility == PostVisibility.Public ||
                    s.Post.UserId == userId ||
                    (s.Post.Visibility == PostVisibility.FollowersOnly && visiblePrivateUserIds.Contains(s.Post.UserId)) ||
                    (s.Post.Visibility == PostVisibility.SelectedFollowers && s.Post.VisibleUsers.Any(v => v.UserId == userId))))
            .OrderByDescending(s => s.CreatedAt)
            .Include(s => s.Post).ThenInclude(p => p.User)
            .Include(s => s.Post).ThenInclude(p => p.Likes)
            .Include(s => s.Post).ThenInclude(p => p.SavedByUsers)
            .Include(s => s.Post).ThenInclude(p => p.VisibleUsers)
            .Include(s => s.Post).ThenInclude(p => p.Comments).ThenInclude(c => c.User)
            .Take(take)
            .Select(s => s.Post)
            .ToListAsync(cancellationToken);
    }

    public async Task<Post?> GetPostAsync(int postId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.SavedByUsers)
            .Include(p => p.VisibleUsers)
            .Include(p => p.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
    }

    public async Task<Post?> GetPostForViewerAsync(int postId, string? viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        List<string> visiblePrivateUserIds = string.IsNullOrEmpty(viewerId)
            ? []
            : await GetFollowingIdsAsync(db, viewerId, cancellationToken);

        return await db.Posts
            .AsNoTracking()
            .Where(p => p.Id == postId)
            .Where(p =>
                (!p.User.IsPrivate || p.UserId == viewerId || visiblePrivateUserIds.Contains(p.UserId)) &&
                (p.Visibility == PostVisibility.Public ||
                    p.UserId == viewerId ||
                    (p.Visibility == PostVisibility.FollowersOnly && visiblePrivateUserIds.Contains(p.UserId)) ||
                    (p.Visibility == PostVisibility.SelectedFollowers && p.VisibleUsers.Any(v => v.UserId == viewerId))))
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.SavedByUsers)
            .Include(p => p.VisibleUsers)
            .Include(p => p.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ToggleLikeAsync(int postId, string userId, CancellationToken cancellationToken = default)
    {
        await rateLimiter.EnsureAllowedAsync(userId, RateLimitPolicies.LikePost, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await CanViewPostAsync(db, postId, userId, cancellationToken))
        {
            throw new InvalidOperationException("Post was not found.");
        }

        var postOwnerId = await db.Posts
            .Where(p => p.Id == postId)
            .Select(p => p.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        var existing = await db.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, cancellationToken);

        if (existing is not null)
        {
            db.Likes.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        db.Likes.Add(new Like
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        if (postOwnerId is not null)
            notificationPublisher.PublishLike(postOwnerId, userId, postId);

        return true;
    }

    public async Task<bool> ToggleSaveAsync(int postId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await CanViewPostAsync(db, postId, userId, cancellationToken))
        {
            throw new InvalidOperationException("Post was not found.");
        }

        var existing = await db.SavedPosts
            .FirstOrDefaultAsync(s => s.PostId == postId && s.UserId == userId, cancellationToken);

        if (existing is not null)
        {
            db.SavedPosts.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
            return false;
        }

        var postExists = await db.Posts.AnyAsync(p => p.Id == postId, cancellationToken);
        if (!postExists)
        {
            throw new InvalidOperationException("Post was not found.");
        }

        db.SavedPosts.Add(new SavedPost
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Comment> AddCommentAsync(
        int postId,
        string userId,
        string content,
        CancellationToken cancellationToken = default)
    {
        await rateLimiter.EnsureAllowedAsync(userId, RateLimitPolicies.CommentPost, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await CanViewPostAsync(db, postId, userId, cancellationToken))
        {
            throw new InvalidOperationException("Post was not found.");
        }

        var postOwnerId = await db.Posts
            .Where(p => p.Id == postId)
            .Select(p => p.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        var comment = new Comment
        {
            PostId = postId,
            UserId = userId,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);

        if (postOwnerId is not null)
            notificationPublisher.PublishComment(postOwnerId, userId, postId);

        return await db.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .FirstAsync(c => c.Id == comment.Id, cancellationToken);
    }

    public async Task<Comment> UpdateCommentAsync(
        int commentId,
        string userId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var trimmed = content.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("Comment cannot be empty.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var comment = await db.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment is null)
        {
            throw new InvalidOperationException("Comment was not found.");
        }

        if (comment.UserId != userId)
        {
            throw new InvalidOperationException("You can only edit your own comments.");
        }

        if (DateTime.UtcNow - comment.CreatedAt > CommentEditWindow)
        {
            throw new InvalidOperationException("Comments can only be edited within 30 minutes.");
        }

        comment.Content = trimmed;
        comment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return comment;
    }

    public async Task DeleteCommentAsync(int commentId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var comment = await db.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment is null)
        {
            return;
        }

        if (comment.UserId != userId && comment.Post.UserId != userId)
        {
            throw new InvalidOperationException("You cannot delete this comment.");
        }

        db.Comments.Remove(comment);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeMediaType(string contentType, string fileName)
    {
        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return "video";
        }

        var extension = Path.GetExtension(fileName);
        return extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webm", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".mov", StringComparison.OrdinalIgnoreCase)
                ? "video"
                : "image";
    }

    private static async Task<List<string>> GetFollowingIdsAsync(
        ApplicationDbContext db,
        string userId,
        CancellationToken cancellationToken)
    {
        return await db.UserFollows
            .AsNoTracking()
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync(cancellationToken);
    }

    private static async Task<bool> CanViewPostAsync(
        ApplicationDbContext db,
        int postId,
        string viewerId,
        CancellationToken cancellationToken)
    {
        return await db.Posts
            .AsNoTracking()
            .AnyAsync(p =>
                p.Id == postId &&
                (!p.User.IsPrivate ||
                    p.UserId == viewerId ||
                    p.User.Followers.Any(f => f.FollowerId == viewerId)) &&
                (p.Visibility == PostVisibility.Public ||
                    p.UserId == viewerId ||
                    (p.Visibility == PostVisibility.FollowersOnly && p.User.Followers.Any(f => f.FollowerId == viewerId)) ||
                    (p.Visibility == PostVisibility.SelectedFollowers && p.VisibleUsers.Any(v => v.UserId == viewerId))),
                cancellationToken);
    }
}
