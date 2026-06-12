namespace MiniInstagram.Models;

public record ChatMessageDto(
    string Id,
    int ConversationId,
    string SenderId,
    string SenderDisplayName,
    string? SenderUserName,
    string Content,
    ChatMessageType MessageType,
    string? MediaPath,
    int? DurationSeconds,
    DateTime SentAt,
    bool IsMine);

public record ConversationListItemDto(
    int ConversationId,
    string OtherUserId,
    string OtherDisplayName,
    string? OtherUserName,
    string? OtherAvatarPath,
    string? LastMessagePreview,
    DateTime? LastMessageAt,
    int UnreadCount);
