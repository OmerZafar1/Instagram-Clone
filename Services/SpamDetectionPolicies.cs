namespace MiniInstagram.Services;

public static class SpamDetectionPolicies
{
    public static readonly RateLimitRule CreatePost = new(
        "spam:post:create",
        RateLimitPolicies.CreatePost.MaxRequests,
        RateLimitPolicies.CreatePost.Window,
        RateLimitPolicies.CreatePost.ErrorMessage);

    public static readonly RateLimitRule CommentPost = new(
        "spam:post:comment",
        RateLimitPolicies.CommentPost.MaxRequests,
        RateLimitPolicies.CommentPost.Window,
        RateLimitPolicies.CommentPost.ErrorMessage);

    public static readonly RateLimitRule SendMessage = new(
        "spam:chat:message",
        RateLimitPolicies.SendMessage.MaxRequests,
        RateLimitPolicies.SendMessage.Window,
        RateLimitPolicies.SendMessage.ErrorMessage);

    public static readonly IReadOnlyList<RateLimitRule> Rules =
    [
        CreatePost,
        CommentPost,
        SendMessage
    ];
}
