using Microsoft.EntityFrameworkCore;
using MiniInstagram.Data;
using MiniInstagram.Data.Mongo.Documents;
using MiniInstagram.Models;
using MiniInstagram.Services.Mongo;

namespace MiniInstagram.Services;

public class ChatService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IChatMessageStore chatMessages,
    INotificationPublisher notificationPublisher,
    IRateLimitService rateLimiter) : IChatService
{
    public async Task<Conversation> GetOrCreateConversationAsync(
        string userId,
        string otherUserId,
        CancellationToken cancellationToken = default)
    {
        if (userId == otherUserId)
        {
            throw new InvalidOperationException("Cannot start a conversation with yourself.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var (user1Id, user2Id) = OrderUserIds(userId, otherUserId);

        var existing = await db.Conversations
            .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var conversation = new Conversation
        {
            User1Id = user1Id,
            User2Id = user2Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<bool> IsParticipantAsync(int conversationId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Conversations.AsNoTracking()
            .AnyAsync(c => c.Id == conversationId && (c.User1Id == userId || c.User2Id == userId), cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationListItemDto>> GetConversationsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var conversations = await db.Conversations
            .AsNoTracking()
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Include(c => c.User1)
            .Include(c => c.User2)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<ConversationListItemDto>();

        foreach (var conversation in conversations)
        {
            var other = conversation.User1Id == userId ? conversation.User2 : conversation.User1;
            var lastMessage = await chatMessages.GetLastMessageAsync(conversation.Id, cancellationToken);
            var unreadCount = await chatMessages.GetUnreadCountAsync(conversation.Id, userId, cancellationToken);

            var preview = lastMessage?.MessageType == ChatMessageType.Voice
                ? "🎤 Voice message"
                : lastMessage?.Content;

            result.Add(new ConversationListItemDto(
                conversation.Id,
                other.Id,
                other.DisplayName,
                other.UserName,
                other.AvatarPath,
                preview,
                lastMessage?.SentAt,
                unreadCount));
        }

        return result;
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(
        int conversationId,
        string userId,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (!await IsParticipantAsync(conversationId, userId, cancellationToken))
        {
            return [];
        }

        var messages = await chatMessages.GetMessagesAsync(conversationId, take, cancellationToken);
        await MarkAsReadAsync(conversationId, userId, cancellationToken);

        if (messages.Count == 0)
        {
            return [];
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
        var senders = await db.Users.AsNoTracking()
            .Where(u => senderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return messages
            .Select(m => ToDto(m, senders.GetValueOrDefault(m.SenderId), userId))
            .ToList();
    }

    public async Task<ChatMessageDto> SendMessageAsync(
        int conversationId,
        string senderId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var trimmed = content.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ArgumentException("Message cannot be empty.", nameof(content));
        }

        if (!await IsParticipantAsync(conversationId, senderId, cancellationToken))
        {
            throw new UnauthorizedAccessException("You are not part of this conversation.");
        }

        await rateLimiter.EnsureAllowedAsync(senderId, RateLimitPolicies.SendMessage, cancellationToken);

        var message = new ChatMessageDocument
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = trimmed,
            MessageType = ChatMessageType.Text,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        return await PersistAsync(message, cancellationToken);
    }

    public async Task<ChatMessageDto> SendVoiceMessageAsync(
        int conversationId,
        string senderId,
        string mediaPath,
        int? durationSeconds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
        {
            throw new ArgumentException("Media path is required.", nameof(mediaPath));
        }

        if (!await IsParticipantAsync(conversationId, senderId, cancellationToken))
        {
            throw new UnauthorizedAccessException("You are not part of this conversation.");
        }

        var message = new ChatMessageDocument
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = string.Empty,
            MessageType = ChatMessageType.Voice,
            MediaPath = mediaPath,
            DurationSeconds = durationSeconds,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        return await PersistAsync(message, cancellationToken);
    }

    private async Task<ChatMessageDto> PersistAsync(ChatMessageDocument message, CancellationToken cancellationToken)
    {
        await chatMessages.InsertAsync(message, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var conversation = await db.Conversations.FirstAsync(c => c.Id == message.ConversationId, cancellationToken);
        conversation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var sender = await db.Users.AsNoTracking().FirstAsync(u => u.Id == message.SenderId, cancellationToken);
        var recipientId = conversation.User1Id == message.SenderId
            ? conversation.User2Id
            : conversation.User1Id;
        var preview = message.MessageType == ChatMessageType.Voice
            ? "🎤 Voice message"
            : Truncate(message.Content, 80);
        notificationPublisher.PublishMessage(recipientId, message.SenderId, conversation.Id, preview);

        return ToDto(message, sender, message.SenderId);
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";

    public async Task MarkAsReadAsync(int conversationId, string userId, CancellationToken cancellationToken = default)
    {
        if (!await IsParticipantAsync(conversationId, userId, cancellationToken))
        {
            return;
        }

        await chatMessages.MarkAsReadAsync(conversationId, userId, cancellationToken);
    }

    private static ChatMessageDto ToDto(ChatMessageDocument message, ApplicationUser? sender, string currentUserId) =>
        new(
            message.Id ?? "",
            message.ConversationId,
            message.SenderId,
            sender?.DisplayName ?? "Unknown",
            sender?.UserName,
            message.Content,
            message.MessageType,
            message.MediaPath,
            message.DurationSeconds,
            message.SentAt,
            message.SenderId == currentUserId);

    private static (string User1Id, string User2Id) OrderUserIds(string a, string b) =>
        string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);
}
