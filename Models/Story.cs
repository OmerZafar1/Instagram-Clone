using MiniInstagram.Data;

namespace MiniInstagram.Models;

public class Story
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string MediaPath { get; set; } = null!;
    public string MediaType { get; set; } = "image";
    public string? Caption { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);

    public ApplicationUser User { get; set; } = null!;
    public ICollection<StoryView> Views { get; set; } = [];
}
