namespace MiniInstagram.Models;

public class PostVisibleUser
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; } = null!;

    public Post Post { get; set; } = null!;
    public Data.ApplicationUser User { get; set; } = null!;
}
