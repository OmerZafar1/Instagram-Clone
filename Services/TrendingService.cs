using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MiniInstagram.Data;
using MiniInstagram.Models;
using StackExchange.Redis;

namespace MiniInstagram.Services;

/// <summary>
/// Trending posts ranked by recent engagement (likes + comments in a time window).
/// Results are cached in Redis to avoid heavy SQL on every Explore load.
/// </summary>
public class TrendingService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IConnectionMultiplexer redis) : ITrendingService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private static string CacheKey(int hours, int take) => $"trending:posts:{hours}h:{take}";

    public async Task<IReadOnlyList<TrendingPostDto>> GetTrendingPostsAsync(
        int hours = 24,
        int take = 12,
        CancellationToken ct = default)
    {
        var key = CacheKey(hours, take);

        if (redis.IsConnected)
        {
            try
            {
                var db = redis.GetDatabase();
                var cached = await db.StringGetAsync(key);
                if (!cached.IsNullOrEmpty)
                {
                    var fromCache = JsonSerializer.Deserialize<List<TrendingPostDto>>(cached!);
                    if (fromCache is not null)
                        return fromCache;
                }
            }
            catch (RedisException) { /* fall through to SQL */ }
            catch (RedisTimeoutException) { }
        }

        var results = await ComputeFromDatabaseAsync(hours, take, ct);

        if (redis.IsConnected)
        {
            try
            {
                var db = redis.GetDatabase();
                var json = JsonSerializer.Serialize(results);
                await db.StringSetAsync(key, json, CacheTtl);
            }
            catch (RedisException) { }
            catch (RedisTimeoutException) { }
        }

        return results;
    }

    private async Task<List<TrendingPostDto>> ComputeFromDatabaseAsync(int hours, int take, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-hours);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var scored = await db.Posts
            .AsNoTracking()
            .Where(p => !p.User.IsPrivate && p.Visibility == PostVisibility.Public)
            .Select(p => new
            {
                p.Id,
                UserName = p.User.UserName ?? "",
                p.User.DisplayName,
                p.User.AvatarPath,
                p.ImagePath,
                p.MediaType,
                p.Caption,
                p.CreatedAt,
                RecentLikes = p.Likes.Count(l => l.CreatedAt >= since),
                RecentComments = p.Comments.Count(c => c.CreatedAt >= since),
                TotalLikes = p.Likes.Count,
                TotalComments = p.Comments.Count
            })
            .Where(p => p.RecentLikes > 0 || p.RecentComments > 0 || p.CreatedAt >= since)
            .ToListAsync(ct);

        return scored
            .Select(p =>
            {
                var score = p.RecentLikes + (p.RecentComments * 2);
                if (p.CreatedAt >= since) score += 1;
                return new TrendingPostDto(
                    p.Id,
                    p.UserName,
                    p.DisplayName,
                    p.AvatarPath,
                    p.ImagePath,
                    p.MediaType,
                    string.IsNullOrWhiteSpace(p.Caption) ? null : p.Caption,
                    p.TotalLikes,
                    p.TotalComments,
                    score,
                    p.CreatedAt);
            })
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.CreatedAt)
            .Take(take)
            .ToList();
    }
}
