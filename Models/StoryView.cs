using MiniInstagram.Data;

namespace MiniInstagram.Models;

public class StoryView
{
    public int Id { get; set; }
    public int StoryId { get; set; }
    public string ViewerId { get; set; } = null!;
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    public Story Story { get; set; } = null!;
    public ApplicationUser Viewer { get; set; } = null!;
}
