namespace MiniInstagram.Models;

public class SavedPost
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Post Post { get; set; } = null!;
    public Data.ApplicationUser User { get; set; } = null!;
}
