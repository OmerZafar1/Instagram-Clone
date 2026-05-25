namespace MiniInstagram.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string SenderId { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;
    public string? MediaPath { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public Data.ApplicationUser Sender { get; set; } = null!;
}
