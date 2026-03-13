using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CoFounderFinder.Api.Models;

public class Message
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public string MatchId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}