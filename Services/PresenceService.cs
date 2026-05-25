using StackExchange.Redis;

namespace MiniInstagram.Services;

/// <summary>
/// Tracks user online/offline state and last-seen time using Redis.
///
/// Redis keys:
///   presence:online:{userId}   → "1"             TTL = 5 minutes (refreshed by heartbeat)
///   presence:lastseen:{userId} → Unix timestamp   no expiry — permanent last-seen record
///
/// If Redis is unavailable the service degrades gracefully: all reads return empty/false,
/// writes are silently swallowed. The rest of the app keeps working normally.
/// </summary>
public class PresenceService(IConnectionMultiplexer redis) : IPresenceService
{
    private static readonly TimeSpan OnlineTtl = TimeSpan.FromMinutes(5);

    private static string OnlineKey(string userId) => $"presence:online:{userId}";
    private static string LastSeenKey(string userId) => $"presence:lastseen:{userId}";

    private bool IsConnected => redis.IsConnected;

    public async Task SetOnlineAsync(string userId, CancellationToken ct = default)
    {
        if (!IsConnected) return;
        try
        {
            var db = redis.GetDatabase();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await db.StringSetAsync(OnlineKey(userId), "1", OnlineTtl);
            await db.StringSetAsync(LastSeenKey(userId), now.ToString());
        }
        catch (RedisException) { /* Redis unavailable — degrade gracefully */ }
        catch (RedisTimeoutException) { }
    }

    public async Task SetOfflineAsync(string userId, CancellationToken ct = default)
    {
        if (!IsConnected) return;
        try
        {
            var db = redis.GetDatabase();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await db.KeyDeleteAsync(OnlineKey(userId));
            await db.StringSetAsync(LastSeenKey(userId), now.ToString());
        }
        catch (RedisException) { }
        catch (RedisTimeoutException) { }
    }

    public async Task<bool> IsOnlineAsync(string userId, CancellationToken ct = default)
    {
        if (!IsConnected) return false;
        try
        {
            var db = redis.GetDatabase();
            return await db.KeyExistsAsync(OnlineKey(userId));
        }
        catch (RedisException) { return false; }
        catch (RedisTimeoutException) { return false; }
    }

    public async Task<DateTime?> GetLastSeenAsync(string userId, CancellationToken ct = default)
    {
        if (!IsConnected) return null;
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync(LastSeenKey(userId));
            if (value.IsNullOrEmpty || !long.TryParse(value, out var unix))
                return null;
            return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        }
        catch (RedisException) { return null; }
        catch (RedisTimeoutException) { return null; }
    }

    public async Task<string> GetPresenceLabelAsync(string userId, CancellationToken ct = default)
    {
        if (!IsConnected) return "";
        try
        {
            if (await IsOnlineAsync(userId, ct))
                return "Active now";

            var lastSeen = await GetLastSeenAsync(userId, ct);
            if (lastSeen is null) return "";

            var ago = DateTime.UtcNow - lastSeen.Value;
            return ago.TotalMinutes < 1  ? "Active just now"
                 : ago.TotalMinutes < 60 ? $"Active {(int)ago.TotalMinutes}m ago"
                 : ago.TotalHours < 24   ? $"Active {(int)ago.TotalHours}h ago"
                 : ago.TotalDays < 7     ? $"Active {(int)ago.TotalDays}d ago"
                 : "";
        }
        catch (RedisException) { return ""; }
        catch (RedisTimeoutException) { return ""; }
    }
}
