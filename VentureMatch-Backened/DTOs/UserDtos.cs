namespace CoFounderFinder.Api.DTOs;

public class UpdateProfileDto
{
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
}