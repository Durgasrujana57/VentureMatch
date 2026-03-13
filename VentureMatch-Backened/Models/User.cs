using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CoFounderFinder.Api.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    // Authentication
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    // Basic Profile
    public string FullName { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    
    // Skills & Interests
    public List<string> Skills { get; set; } = new();
    public List<string> Interests { get; set; } = new();
    
    // Availability
    public string Availability { get; set; } = "Part-time";
    
    // What they're looking for
    public List<string> LookingFor { get; set; } = new();
    
    // Social Links
    public string? GithubUrl { get; set; }
    public string? LinkedinUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}