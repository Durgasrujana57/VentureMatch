using System.ComponentModel.DataAnnotations;

namespace CoFounderFinder.Api.DTOs;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    public string FullName { get; set; } = string.Empty;
    
    public string Headline { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string>? Skills { get; set; }
    public List<string>? Interests { get; set; }
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}