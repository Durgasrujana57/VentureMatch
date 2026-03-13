using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CoFounderFinder.Api.Models;

public class Match
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public string User1Id { get; set; } = string.Empty;
    public string User2Id { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public int MatchScore { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}