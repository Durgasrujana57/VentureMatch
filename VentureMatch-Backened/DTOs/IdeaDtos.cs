using System.ComponentModel.DataAnnotations;

namespace CoFounderFinder.Api.DTOs;

public class CreateIdeaDto
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public string ProblemStatement { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Stage { get; set; } = "Idea";
    
    public List<RoleNeededDto> RolesNeeded { get; set; } = new();
    public List<string> LookingForSkills { get; set; } = new();
}

public class UpdateIdeaDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public List<RoleNeededDto> RolesNeeded { get; set; } = new();
    public List<string> LookingForSkills { get; set; } = new();
}

public class RoleNeededDto
{
    public string Role { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public string Commitment { get; set; } = "Part-time";
}

public class IdeaResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public List<RoleNeededDto> RolesNeeded { get; set; } = new();
    public List<string> LookingForSkills { get; set; } = new();
    public UserSummaryDto? User { get; set; }
    public int Views { get; set; }
    public int Applications { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class IdeaSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
}

public class ApplyToIdeaDto
{
    [Required]
    public string IdeaId { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
}

public class ApplicationResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string IdeaId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public UserSummaryDto? User { get; set; }
    public IdeaSummaryDto? Idea { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
}

// ADD THIS NEW DTO
public class UpdateApplicationStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}