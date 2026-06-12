namespace MiniInstagram.Models;

public class Conversation
{
    public int Id { get; set; }
    public string User1Id { get; set; } = null!;
    public string User2Id { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Data.ApplicationUser User1 { get; set; } = null!;
    public Data.ApplicationUser User2 { get; set; } = null!;
}
