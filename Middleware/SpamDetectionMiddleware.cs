using System.Security.Claims;
using System.Text.Json;
using MiniInstagram.Services;

namespace MiniInstagram.Middleware;

public class SpamDetectionMiddleware(
    RequestDelegate next,
    ILogger<SpamDetectionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimiter)
    {
        var rule = ResolveRule(context.Request);
        if (rule is null)
        {
            await next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await next(context);
            return;
        }

        var isAllowed = await rateLimiter.IsAllowedAsync(
            userId,
            rule.Action,
            rule.MaxRequests,
            rule.Window,
            context.RequestAborted);

        if (isAllowed)
        {
            await next(context);
            return;
        }

        logger.LogWarning("Spam detector blocked user {UserId} for action {Action}.", userId, rule.Action);
        await RejectAsync(context, rule);
    }

    private static RateLimitRule? ResolveRule(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return null;
        }

        var path = request.Path.Value ?? string.Empty;

        if (path.Equals("/posts/create", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/posts", StringComparison.OrdinalIgnoreCase))
        {
            return SpamDetectionPolicies.CreatePost;
        }

        if (path.StartsWith("/api/posts/", StringComparison.OrdinalIgnoreCase) &&
            path.EndsWith("/comments", StringComparison.OrdinalIgnoreCase))
        {
            return SpamDetectionPolicies.CommentPost;
        }

        return null;
    }

    private static async Task RejectAsync(HttpContext context, RateLimitRule rule)
    {
        if (IsFormPost(context.Request))
        {
            context.Response.Redirect($"/create?error=spam&action={Uri.EscapeDataString(rule.Action)}");
            return;
        }

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/json";
        context.Response.Headers.RetryAfter = Math.Ceiling(rule.Window.TotalSeconds).ToString("0");

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            error = rule.ErrorMessage,
            action = rule.Action,
            limit = rule.MaxRequests,
            windowSeconds = (int)rule.Window.TotalSeconds
        }));
    }

    private static bool IsFormPost(HttpRequest request) =>
        request.Path.Equals("/posts/create", StringComparison.OrdinalIgnoreCase);
}
