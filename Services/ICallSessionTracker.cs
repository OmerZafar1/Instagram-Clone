namespace MiniInstagram.Services;

public interface ICallSessionTracker
{
    void RegisterConnection(string userId, string connectionId);
    void RemoveConnection(string connectionId);
    string? GetConnectionId(string userId);
    string StartCall(string callerId, string callerConnectionId, string targetUserId, bool isVideoCall);
    bool TryGetCall(string callId, out CallSession? session);
    void RemoveCall(string callId);
}

public sealed class CallSession
{
    public required string CallId { get; init; }
    public required string CallerId { get; init; }
    public required string TargetUserId { get; init; }
    public required string CallerConnectionId { get; init; }
    public bool IsVideoCall { get; init; } = true;
    public bool CalleeAccepted { get; set; }
}
