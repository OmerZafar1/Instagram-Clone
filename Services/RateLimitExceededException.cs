namespace MiniInstagram.Services;

public class RateLimitExceededException(RateLimitRule rule)
    : InvalidOperationException(rule.ErrorMessage)
{
    public RateLimitRule Rule { get; } = rule;
}
