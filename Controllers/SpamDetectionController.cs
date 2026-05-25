using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniInstagram.Services;

namespace MiniInstagram.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/spam-detection")]
public class SpamDetectionController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult Status() => Ok(new
    {
        enabled = true,
        storage = "Redis fixed-window counters",
        middlewareCoverage = new[]
        {
            "POST /posts/create",
            "POST /api/posts",
            "POST /api/posts/{postId}/comments"
        },
        serviceCoverage = new[]
        {
            "Blazor comments",
            "SignalR chat messages",
            "post creation service calls"
        }
    });

    [HttpGet("policies")]
    public IActionResult Policies() => Ok(SpamDetectionPolicies.Rules.Select(rule => new
    {
        rule.Action,
        rule.MaxRequests,
        WindowSeconds = (int)rule.Window.TotalSeconds,
        rule.ErrorMessage
    }));
}
