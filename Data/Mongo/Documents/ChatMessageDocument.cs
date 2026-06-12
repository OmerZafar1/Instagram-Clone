using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MiniInstagram.Models;

namespace MiniInstagram.Data.Mongo.Documents;

public class ChatMessageDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public int ConversationId { get; set; }
    public string SenderId { get; set; } = "";
    public string Content { get; set; } = "";
    public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;
    public string? MediaPath { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
