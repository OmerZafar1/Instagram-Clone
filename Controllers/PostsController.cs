using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniInstagram.Models;
using MiniInstagram.Services;

namespace MiniInstagram.Controllers;

[Authorize]
[Route("posts")]
public class PostsController(IPostService postService) : Controller
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".mp4", ".webm", ".mov"
    };

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        [FromForm] string? caption,
        [FromForm] IFormFile? media,
        [FromForm] PostVisibility visibility = PostVisibility.Public,
        [FromForm] List<string>? selectedViewerIds = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        if (media is null || media.Length == 0)
        {
            return Redirect("/create?error=noimage");
        }

        if (!IsAllowedMedia(media))
        {
            return Redirect("/create?error=type");
        }

        try
        {
            await using var stream = media.OpenReadStream();
            await postService.CreatePostAsync(
                userId,
                caption ?? string.Empty,
                stream,
                media.FileName,
                media.ContentType,
                visibility,
                selectedViewerIds,
                cancellationToken);
        }
        catch (RateLimitExceededException)
        {
            return Redirect("/create?error=ratelimit");
        }

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
