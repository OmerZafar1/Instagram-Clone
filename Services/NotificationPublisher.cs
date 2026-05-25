using Hangfire;
using MiniInstagram.Jobs;

namespace MiniInstagram.Services;

public class NotificationPublisher(IBackgroundJobClient jobs) : INotificationPublisher
{
    public void PublishLike(string recipientId, string actorId, int postId) =>
        jobs.Enqueue<NotificationJob>(j => j.SendLikeAsync(recipientId, actorId, postId));

    public void PublishComment(string recipientId, string actorId, int postId) =>
        jobs.Enqueue<NotificationJob>(j => j.SendCommentAsync(recipientId, actorId, postId));

    public void PublishFollow(string recipientId, string actorId) =>
        jobs.Enqueue<NotificationJob>(j => j.SendFollowAsync(recipientId, actorId));

    public void PublishMessage(string recipientId, string actorId, int conversationId, string preview) =>
        jobs.Enqueue<NotificationJob>(j => j.SendMessageAsync(recipientId, actorId, conversationId, preview));
}
