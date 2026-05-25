namespace MiniInstagram.Models;

public class FollowRequest
{
    public int Id { get; set; }
    public string RequesterId { get; set; } = null!;
    public string TargetUserId { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Data.ApplicationUser Requester { get; set; } = null!;
    public Data.ApplicationUser TargetUser { get; set; } = null!;
}
