using MiniInstagram.Models;

namespace MiniInstagram.Services;

public interface IChatService
{
    Task<Conversation> GetOrCreateConversationAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);
    Task<bool> IsParticipantAsync(int conversationId, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationListItemDto>> GetConversationsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(int conversationId, string userId, int take = 100, CancellationToken cancellationToken = default);
    Task<ChatMessageDto> SendMessageAsync(int conversationId, string senderId, string content, CancellationToken cancellationToken = default);
    Task<ChatMessageDto> SendVoiceMessageAsync(int conversationId, string senderId, string mediaPath, int? durationSeconds, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int conversationId, string userId, CancellationToken cancellationToken = default);
}
