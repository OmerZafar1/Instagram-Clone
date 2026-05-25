using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using MiniInstagram.Data;
using MiniInstagram.Models;
using MiniInstagram.Services;

namespace MiniInstagram.Hubs;

[Authorize]
public class CallHub(
    ICallSessionTracker callTracker,
    IPresenceService presence,
    IServiceScopeFactory scopeFactory) : Hub
{
    private static string GroupName(string callId) => $"call-{callId}";

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        callTracker.RegisterConnection(userId, Context.ConnectionId);
        await presence.SetOnlineAsync(userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        callTracker.RemoveConnection(Context.ConnectionId);
        await presence.SetOfflineAsync(userId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Called by the client every 2 minutes to refresh the online TTL.</summary>
    public Task KeepAlive() => presence.SetOnlineAsync(GetUserId());

    public async Task<string> StartCall(string targetUserId, bool isVideoCall = true)
    {
        var callerId = GetUserId();
        if (callerId == targetUserId)
            throw new HubException("You cannot call yourself.");

        var targetConnection = callTracker.GetConnectionId(targetUserId);
        if (targetConnection is null)
            throw new HubException("User is offline or not available for calls.");

        var callId = callTracker.StartCall(callerId, Context.ConnectionId, targetUserId, isVideoCall);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(callId));

        var caller = await GetUserAsync(callerId);
        var dto = new IncomingCallDto(callId, callerId, caller.UserName ?? "", caller.DisplayName, isVideoCall);
        await Clients.Client(targetConnection).SendAsync("IncomingCall", dto);
        return callId;
    }

    public async Task AcceptCall(string callId)
    {
        var userId = GetUserId();
        if (!callTracker.TryGetCall(callId, out var session) || session is null || session.TargetUserId != userId)
            throw new HubException("Invalid call.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(callId));

        if (!session.CalleeAccepted)
        {
            session.CalleeAccepted = true;
            await Clients.Client(session.CallerConnectionId).SendAsync("CallAccepted", callId);
        }

        await Clients.Caller.SendAsync("CallAccepted", callId);
    }

    public async Task DeclineCall(string callId)
    {
        var userId = GetUserId();
        if (!callTracker.TryGetCall(callId, out var session) || session is null || session.TargetUserId != userId)
            throw new HubException("Invalid call.");

        await Clients.Client(session.CallerConnectionId).SendAsync("CallDeclined", callId);
        callTracker.RemoveCall(callId);
    }

    public Task SendOffer(string callId, string sdp) =>
        Clients.OthersInGroup(GroupName(callId)).SendAsync("ReceiveOffer", sdp);

    public Task SendAnswer(string callId, string sdp) =>
        Clients.OthersInGroup(GroupName(callId)).SendAsync("ReceiveAnswer", sdp);

    public Task SendIceCandidate(string callId, string candidate) =>
        Clients.OthersInGroup(GroupName(callId)).SendAsync("ReceiveIceCandidate", candidate);

    public async Task EndCall(string callId)
    {
        await Clients.Group(GroupName(callId)).SendAsync("CallEnded", callId);
        callTracker.RemoveCall(callId);
    }

    private string GetUserId() =>
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new HubException("Not authenticated.");

    private async Task<ApplicationUser> GetUserAsync(string userId)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await userManager.FindByIdAsync(userId)
            ?? throw new HubException("User not found.");
    }
}
