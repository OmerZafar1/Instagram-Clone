namespace MiniInstagram.Services;

public interface IRateLimitService
{
    /// <summary>
    /// Returns true if the action is allowed, false if the rate limit is exceeded.
    /// Uses a fixed-window counter in Redis.
    /// </summary>
    Task<bool> IsAllowedAsync(string userId, string action, int maxRequests, TimeSpan window, CancellationToken ct = default);

    Task EnsureAllowedAsync(string userId, RateLimitRule rule, CancellationToken ct = default);
}
