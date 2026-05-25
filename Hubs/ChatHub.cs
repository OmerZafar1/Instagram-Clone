using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MiniInstagram.Models;
using MiniInstagram.Services;

namespace MiniInstagram.Hubs;

[Authorize]
public class ChatHub(IChatService chatService) : Hub
{
    public static string GetGroupName(int conversationId) => $"conversation-{conversationId}";

    public async Task JoinConversation(int conversationId)
    {
        var userId = GetUserId();
        if (!await chatService.IsParticipantAsync(conversationId, userId))
        {
            throw new HubException("You do not have access to this conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(conversationId));
    }

    public async Task SendMessage(int conversationId, string content)
    {
        var userId = GetUserId();
        ChatMessageDto message;
        try
        {
            message = await chatService.SendMessageAsync(conversationId, userId, content);
        }
        catch (RateLimitExceededException ex)
        {
            throw new HubException(ex.Message);
        }

        await Clients.Group(GetGroupName(conversationId))
            .SendAsync("ReceiveMessage", message);
    }

    private string GetUserId() =>
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new HubException("User is not authenticated.");
}
