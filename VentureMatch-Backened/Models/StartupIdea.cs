using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CoFounderFinder.Api.Models;

public class StartupIdea
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Stage { get; set; } = "Idea"; // Idea, MVP, Prototype, Launched
    
    public List<RoleNeeded> RolesNeeded { get; set; } = new();
    public List<string> LookingForSkills { get; set; } = new();
    
    public int Views { get; set; } = 0;
    public int Applications { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class RoleNeeded
{
    public string Role { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public string Commitment { get; set; } = "Part-time";
    public bool IsFilled { get; set; } = false;
}

public class IdeaApplication
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public string IdeaId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, accepted, rejected
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}