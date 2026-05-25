using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiniInstagram.Hubs;
using MiniInstagram.Services;

namespace MiniInstagram.Controllers;

[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("api/chat/voice")]
public class ChatVoiceController(
    IChatService chatService,
    IImageStorageService storage,
    IHubContext<ChatHub> chatHub,
    IRateLimitService rateLimiter) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] int conversationId,
        [FromForm] int? durationSeconds,
        IFormFile audio,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        if (audio is null || audio.Length == 0)
        {
            return BadRequest("Audio file is required.");
        }

        if (!await chatService.IsParticipantAsync(conversationId, userId, cancellationToken))
        {
            return Forbid();
        }

        if (!await rateLimiter.IsAllowedAsync(
                userId,
                RateLimitPolicies.SendVoiceMessage.Action,
                RateLimitPolicies.SendVoiceMessage.MaxRequests,
                RateLimitPolicies.SendVoiceMessage.Window,
                cancellationToken))
        {
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new { error = RateLimitPolicies.SendVoiceMessage.ErrorMessage });
        }

        var extension = Path.GetExtension(audio.FileName);
        if (string.IsNullOrEmpty(extension))
        {
            extension = audio.ContentType switch
            {
                "audio/webm" => ".webm",
                "audio/mp4" => ".m4a",
                "audio/mpeg" => ".mp3",
                "audio/wav" => ".wav",
                "audio/ogg" => ".ogg",
                _ => ".webm"
            };
        }

        await using var stream = audio.OpenReadStream();
        var mediaPath = await storage.SaveVoiceAsync(stream, $"voice{extension}", cancellationToken);

        var dto = await chatService.SendVoiceMessageAsync(conversationId, userId, mediaPath, durationSeconds, cancellationToken);

        await chatHub.Clients
            .Group(ChatHub.GetGroupName(conversationId))
            .SendAsync("ReceiveMessage", dto, cancellationToken);

        return Ok(dto);
    }
}
