namespace CoFounderFinder.Api.DTOs;

// Auth Response
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime Expiry { get; set; }
}

// User Responses
public class UserProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<string> Interests { get; set; } = new();
    public string Availability { get; set; } = string.Empty;
    public List<string> LookingFor { get; set; } = new();
    public string? GithubUrl { get; set; }
    public string? LinkedinUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<string> Interests { get; set; } = new();
    public string Availability { get; set; } = string.Empty;
}

// Matching Responses
public class PotentialMatchResponse
{
    public UserSummaryDto User { get; set; } = new();
    public int MatchScore { get; set; }
    public string MatchReason { get; set; } = string.Empty;
}

public class LikeResponse
{
    public bool IsMatch { get; set; }
    public string? MatchId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class MatchResponse
{
    public string MatchId { get; set; } = string.Empty;
    public UserSummaryDto User { get; set; } = new();
    public DateTime MatchedAt { get; set; }
}

// Message Responses
public class MessageResponse
{
    public string Id { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsMine { get; set; }
}

public class UnreadCountResponse
{
    public int Count { get; set; }
}

// Error Response
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}