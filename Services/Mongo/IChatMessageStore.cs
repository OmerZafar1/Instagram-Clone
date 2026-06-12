using MiniInstagram.Data.Mongo.Documents;

namespace MiniInstagram.Services.Mongo;

public interface IChatMessageStore
{
    Task<ChatMessageDocument?> GetLastMessageAsync(int conversationId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int conversationId, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessageDocument>> GetMessagesAsync(int conversationId, int take, CancellationToken cancellationToken = default);
    Task<ChatMessageDocument> InsertAsync(ChatMessageDocument message, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int conversationId, string userId, CancellationToken cancellationToken = default);
}
