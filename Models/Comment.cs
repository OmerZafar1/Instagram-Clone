namespace MiniInstagram.Models;

public class Comment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Post Post { get; set; } = null!;
    public Data.ApplicationUser User { get; set; } = null!;
}
