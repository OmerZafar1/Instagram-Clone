namespace MiniInstagram.Services;

public sealed record RateLimitRule(
    string Action,
    int MaxRequests,
    TimeSpan Window,
    string ErrorMessage);

public static class RateLimitPolicies
{
    public static readonly RateLimitRule CreatePost = new(
        "post:create",
        5,
        TimeSpan.FromMinutes(2),
        "You are creating posts too quickly. Please wait a few minutes and try again.");

    public static readonly RateLimitRule CreateStory = new(
        "story:create",
        10,
        TimeSpan.FromHours(1),
        "You are creating stories too quickly. Please wait before posting another story.");

    public static readonly RateLimitRule LikePost = new(
        "post:like",
        60,
        TimeSpan.FromMinutes(1),
        "You are liking posts too quickly. Please slow down.");

    public static readonly RateLimitRule CommentPost = new(
        "post:comment",
        5,
        TimeSpan.FromMinutes(1),
        "You are commenting too quickly. Please slow down.");

    public static readonly RateLimitRule FollowUser = new(
        "user:follow",
        30,
        TimeSpan.FromHours(1),
        "You are following or unfollowing people too quickly. Please wait before trying again.");

    public static readonly RateLimitRule SendMessage = new(
        "chat:message",
        10,
        TimeSpan.FromMinutes(1),
        "You are sending messages too quickly. Please slow down.");

    public static readonly RateLimitRule SendVoiceMessage = new(
        "chat:voice",
        15,
        TimeSpan.FromMinutes(5),
        "You are sending voice messages too quickly. Please wait a little.");

    public static readonly RateLimitRule StoryReaction = new(
        "story:reaction",
        30,
        TimeSpan.FromMinutes(1),
        "You are reacting to stories too quickly. Please slow down.");

    public static readonly RateLimitRule StoryReply = new(
        "story:reply",
        20,
        TimeSpan.FromMinutes(1),
        "You are replying to stories too quickly. Please slow down.");
}
