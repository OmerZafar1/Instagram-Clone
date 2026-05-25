using MiniInstagram.Data;

namespace MiniInstagram.Models;

public class Notification
{
    public int Id { get; set; }
    public string RecipientId { get; set; } = "";
    public string ActorId { get; set; } = "";
    public NotificationType Type { get; set; }
    public string Message { get; set; } = "";
    public int? PostId { get; set; }
    public int? ConversationId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Recipient { get; set; } = default!;
    public ApplicationUser Actor { get; set; } = default!;
}
