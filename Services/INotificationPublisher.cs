namespace MiniInstagram.Services;

/// <summary>Enqueues notification work to Hangfire (runs outside the HTTP request).</summary>
public interface INotificationPublisher
{
    void PublishLike(string recipientId, string actorId, int postId);
    void PublishComment(string recipientId, string actorId, int postId);
    void PublishFollow(string recipientId, string actorId);
    void PublishMessage(string recipientId, string actorId, int conversationId, string preview);
}
