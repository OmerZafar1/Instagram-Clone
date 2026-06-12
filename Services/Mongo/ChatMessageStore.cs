using MiniInstagram.Data.Mongo.Documents;
using MongoDB.Driver;

namespace MiniInstagram.Services.Mongo;

public class ChatMessageStore(IMongoDatabase database) : IChatMessageStore
{
    private readonly IMongoCollection<ChatMessageDocument> _messages =
        database.GetCollection<ChatMessageDocument>("chat_messages");

    public async Task<ChatMessageDocument?> GetLastMessageAsync(
        int conversationId,
        CancellationToken cancellationToken = default) =>
        await _messages.Find(m => m.ConversationId == conversationId)
            .SortByDescending(m => m.SentAt)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetUnreadCountAsync(
        int conversationId,
        string userId,
        CancellationToken cancellationToken = default) =>
        (int)await _messages.CountDocumentsAsync(
            m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead,
            cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<ChatMessageDocument>> GetMessagesAsync(
        int conversationId,
        int take,
        CancellationToken cancellationToken = default) =>
        await _messages.Find(m => m.ConversationId == conversationId)
            .SortBy(m => m.SentAt)
            .Limit(take)
            .ToListAsync(cancellationToken);

    public async Task<ChatMessageDocument> InsertAsync(
        ChatMessageDocument message,
        CancellationToken cancellationToken = default)
    {
        await _messages.InsertOneAsync(message, cancellationToken: cancellationToken);
        return message;
    }

    public async Task MarkAsReadAsync(
        int conversationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<ChatMessageDocument>.Filter.And(
            Builders<ChatMessageDocument>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<ChatMessageDocument>.Filter.Ne(m => m.SenderId, userId),
            Builders<ChatMessageDocument>.Filter.Eq(m => m.IsRead, false));

        var update = Builders<ChatMessageDocument>.Update.Set(m => m.IsRead, true);
        await _messages.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }
}
