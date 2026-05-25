namespace MiniInstagram.Services;

public interface IPresenceService
{
    /// <summary>Mark user as online and refresh their TTL.</summary>
    Task SetOnlineAsync(string userId, CancellationToken ct = default);

    /// <summary>Remove user from online set immediately (on disconnect).</summary>
    Task SetOfflineAsync(string userId, CancellationToken ct = default);

    /// <summary>Returns null if offline, otherwise the last-seen UTC time.</summary>
    Task<DateTime?> GetLastSeenAsync(string userId, CancellationToken ct = default);

    /// <summary>Human-readable label: "Active now", "Active 5m ago", etc.</summary>
    Task<string> GetPresenceLabelAsync(string userId, CancellationToken ct = default);

    /// <summary>Check if a user is currently online.</summary>
    Task<bool> IsOnlineAsync(string userId, CancellationToken ct = default);
}
