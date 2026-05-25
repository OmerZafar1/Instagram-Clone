namespace MiniInstagram.Models;

public class UserFollow
{
    public int Id { get; set; }
    public string FollowerId { get; set; } = null!;
    public string FollowingId { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Data.ApplicationUser Follower { get; set; } = null!;
    public Data.ApplicationUser Following { get; set; } = null!;
}
