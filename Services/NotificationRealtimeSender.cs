using Microsoft.AspNetCore.SignalR;
using MiniInstagram.Hubs;
using MiniInstagram.Models;

namespace MiniInstagram.Services;

public class NotificationRealtimeSender(IHubContext<NotificationHub> hub) : INotificationRealtimeSender
{
    public Task SendToUserAsync(string recipientId, NotificationDto notification, CancellationToken ct = default) =>
        hub.Clients.Group(NotificationHub.UserGroup(recipientId))
            .SendAsync("ReceiveNotification", notification, ct);

    public Task SendUnreadCountAsync(string recipientId, int count, CancellationToken ct = default) =>
        hub.Clients.Group(NotificationHub.UserGroup(recipientId))
            .SendAsync("UnreadCountChanged", count, ct);
}
