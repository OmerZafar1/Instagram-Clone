using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniInstagram.Models;
using MiniInstagram.Services;

namespace MiniInstagram.Controllers;

[ApiController]
[Route("api/posts")]
[Authorize]
public class PostsApiController(IPostService postService) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".mp4", ".webm", ".mov"
    };

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        [FromForm] string caption,
        IFormFile media,
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
            return BadRequest("Media file is required.");
        }

        if (!IsAllowedMedia(media))
        {
            return BadRequest("Posts support JPG, PNG, WEBP, GIF, MP4, WEBM, or MOV files.");
        }

        Post post;
        try
        {
            await using var stream = media.OpenReadStream();
            post = await postService.CreatePostAsync(
                userId,
                caption,
                stream,
                media.FileName,
                media.ContentType,
                visibility,
                selectedViewerIds,
                cancellationToken);
        }
        catch (RateLimitExceededException ex)
        {
            return TooManyRequests(ex.Rule);
        }

        return Created($"/api/posts/{post.Id}", new
        {
            post.Id,
            post.Caption,
            post.ImagePath,
            post.MediaType,
            post.Visibility,
            post.CreatedAt
        });
    }

    [HttpGet("feed")]
    public async Task<IActionResult> Feed([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var posts = await postService.GetFeedAsync(userId, take, cancellationToken);
        return Ok(posts.Select(MapPost));
    }

    [HttpPost("{postId:int}/like")]
    public async Task<IActionResult> ToggleLike(int postId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var liked = await postService.ToggleLikeAsync(postId, userId, cancellationToken);
            return Ok(new { liked });
        }
        catch (RateLimitExceededException ex)
        {
            return TooManyRequests(ex.Rule);
        }
    }

    [HttpPost("{postId:int}/comments")]
    public async Task<IActionResult> AddComment(
        int postId,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Comment cannot be empty.");
        }

        try
        {
            var comment = await postService.AddCommentAsync(postId, userId, request.Content, cancellationToken);
            return Ok(new
            {
                comment.Id,
                comment.Content,
                comment.CreatedAt,
                User = new
                {
                    comment.User.UserName,
                    comment.User.DisplayName,
                    comment.User.AvatarPath
                }
            });
        }
        catch (RateLimitExceededException ex)
        {
            return TooManyRequests(ex.Rule);
        }
    }

    private ObjectResult TooManyRequests(RateLimitRule rule) =>
        StatusCode(StatusCodes.Status429TooManyRequests, new { error = rule.ErrorMessage });

    private static object MapPost(Models.Post post) => new
    {
        post.Id,
        post.Caption,
        post.ImagePath,
        post.MediaType,
        post.Visibility,
        post.CreatedAt,
        Author = new
        {
            post.User.UserName,
            post.User.DisplayName,
            post.User.AvatarPath
        },
        LikeCount = post.Likes.Count,
        CommentCount = post.Comments.Count,
        Comments = post.Comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Content,
                c.CreatedAt,
                User = new { c.User.UserName, c.User.DisplayName }
            })
    };

    public record AddCommentRequest(string Content);

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
