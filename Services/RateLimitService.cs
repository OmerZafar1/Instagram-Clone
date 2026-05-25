using System.Collections.Concurrent;
using StackExchange.Redis;

namespace MiniInstagram.Services;

/// <summary>
/// Fixed-window rate limiter backed by Redis.
/// Redis key pattern: ratelimit:{action}:{userId}:{window-bucket}
/// </summary>
public class RateLimitService(
    IConnectionMultiplexer redis,
    ILogger<RateLimitService> logger) : IRateLimitService
{
    private readonly ConcurrentDictionary<string, LocalCounter> fallbackCounters = new();

    public async Task<bool> IsAllowedAsync(
        string userId,
        string action,
        int maxRequests,
        TimeSpan window,
        CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (long)window.TotalSeconds;
            var key = $"ratelimit:{action}:{userId}:{bucket}";

            var count = await db.StringIncrementAsync(key);
            if (count == 1)
            {
                await db.KeyExpireAsync(key, window);
            }

            return count <= maxRequests;
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis rate limit check failed for action {Action}. Using local fallback limiter.", action);
            return IsAllowedByFallback(userId, action, maxRequests, window);
        }
        catch (RedisTimeoutException ex)
        {
            logger.LogWarning(ex, "Redis rate limit check timed out for action {Action}. Using local fallback limiter.", action);
            return IsAllowedByFallback(userId, action, maxRequests, window);
        }
    }

    public async Task EnsureAllowedAsync(string userId, RateLimitRule rule, CancellationToken ct = default)
    {
        if (!await IsAllowedAsync(userId, rule.Action, rule.MaxRequests, rule.Window, ct))
        {
            throw new RateLimitExceededException(rule);
        }
    }

    private bool IsAllowedByFallback(string userId, string action, int maxRequests, TimeSpan window)
    {
        var now = DateTimeOffset.UtcNow;
        var bucket = now.ToUnixTimeSeconds() / (long)window.TotalSeconds;
        var key = $"fallback:{action}:{userId}:{bucket}";

        var counter = fallbackCounters.AddOrUpdate(
            key,
            _ => new LocalCounter(1, now.Add(window)),
            (_, existing) => existing.ExpiresAt <= now
                ? new LocalCounter(1, now.Add(window))
                : existing with { Count = existing.Count + 1 });

        CleanupExpiredFallbackCounters(now);
        return counter.Count <= maxRequests;
    }

    private void CleanupExpiredFallbackCounters(DateTimeOffset now)
    {
        foreach (var (key, counter) in fallbackCounters)
        {
            if (counter.ExpiresAt <= now)
            {
                fallbackCounters.TryRemove(key, out _);
            }
        }
    }

    private sealed record LocalCounter(int Count, DateTimeOffset ExpiresAt);
}
