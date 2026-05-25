using System.Collections.Concurrent;

namespace MiniInstagram.Services;

public class CallSessionTracker : ICallSessionTracker
{
    private readonly ConcurrentDictionary<string, string> _connections = new();
    private readonly ConcurrentDictionary<string, CallSession> _calls = new();

    public void RegisterConnection(string userId, string connectionId) =>
        _connections[userId] = connectionId;

    public void RemoveConnection(string connectionId)
    {
        foreach (var pair in _connections)
        {
            if (pair.Value == connectionId)
            {
                _connections.TryRemove(pair.Key, out _);
                break;
            }
        }
    }

    public string? GetConnectionId(string userId) =>
        _connections.TryGetValue(userId, out var connectionId) ? connectionId : null;

    public string StartCall(string callerId, string callerConnectionId, string targetUserId, bool isVideoCall)
    {
        var callId = Guid.NewGuid().ToString("N");
        _calls[callId] = new CallSession
        {
            CallId = callId,
            CallerId = callerId,
            TargetUserId = targetUserId,
            CallerConnectionId = callerConnectionId,
            IsVideoCall = isVideoCall,
            CalleeAccepted = false
        };
        return callId;
    }

    public bool TryGetCall(string callId, out CallSession? session) =>
        _calls.TryGetValue(callId, out session);

    public void RemoveCall(string callId) => _calls.TryRemove(callId, out _);
}
