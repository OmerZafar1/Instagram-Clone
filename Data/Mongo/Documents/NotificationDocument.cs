using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MiniInstagram.Models;

namespace MiniInstagram.Data.Mongo.Documents;

public class NotificationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string RecipientId { get; set; } = "";
    public string ActorId { get; set; } = "";
    public NotificationType Type { get; set; }
    public string Message { get; set; } = "";
    public int? PostId { get; set; }
    public int? ConversationId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
