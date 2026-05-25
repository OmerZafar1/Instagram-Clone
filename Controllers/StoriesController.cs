using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniInstagram.Services;

namespace MiniInstagram.Controllers;

[Authorize]
[Route("stories")]
public class StoriesController(
    IStoryService storyService,
    IRateLimitService rateLimiter) : Controller
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".mp4", ".webm", ".mov"
    };

    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        [FromForm] string? caption,
        [FromForm] IFormFile? media,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await rateLimiter.IsAllowedAsync(
                userId,
                RateLimitPolicies.CreateStory.Action,
                RateLimitPolicies.CreateStory.MaxRequests,
                RateLimitPolicies.CreateStory.Window,
                cancellationToken))
        {
            return Redirect("/stories/create?error=ratelimit");
        }

        if (media is null || media.Length == 0)
        {
            return Redirect("/stories/create?error=nomedia");
        }

        if (!IsAllowedMedia(media))
        {
            return Redirect("/stories/create?error=type");
        }

        await using var stream = media.OpenReadStream();
        await storyService.CreateStoryAsync(
            userId,
            caption,
            stream,
            media.FileName,
            media.ContentType,
            cancellationToken);

        return Redirect("/feed");
    }

    private static bool IsAllowedMedia(IFormFile media)
    {
        var extension = Path.GetExtension(media.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return false;
        }

        return media.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || media.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
    }
}
