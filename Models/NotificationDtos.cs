namespace MiniInstagram.Models;

public record NotificationDto(
    int Id,
    string ActorId,
    string ActorUserName,
    string ActorDisplayName,
    string? ActorAvatarPath,
    NotificationType Type,
    string Message,
    int? PostId,
    int? ConversationId,
    bool IsRead,
    DateTime CreatedAt);
