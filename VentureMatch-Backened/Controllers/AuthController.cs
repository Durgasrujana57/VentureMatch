using CoFounderFinder.Api.DTOs;
using CoFounderFinder.Api.Helpers;
using CoFounderFinder.Api.Models;
using CoFounderFinder.Api.Services;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CoFounderFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MongoDbService _mongoDbService;
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        MongoDbService mongoDbService, 
        JwtHelper jwtHelper,
        ILogger<AuthController> logger)
    {
        _mongoDbService = mongoDbService;
        _jwtHelper = jwtHelper;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        try
        {
            _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

            var existingUser = await _mongoDbService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
                return BadRequest(new ErrorResponse { Message = "User with this email already exists" });

            var user = new User
            {
                Email = request.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName.Trim(),
                Headline = request.Headline ?? "",
                Bio = "",
                Location = request.Location ?? "",
                Skills = request.Skills ?? new List<string>(),
                Interests = request.Interests ?? new List<string>(),
                Availability = "Part-time",
                LookingFor = new List<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _mongoDbService.CreateUserAsync(user);
            _logger.LogInformation("User registered successfully: {UserId}", user.Id);

            var token = _jwtHelper.GenerateToken(user);
            var response = new AuthResponseDto
            {
                Token = token,
                UserId = user.Id!,
                Email = user.Email,
                FullName = user.FullName,
                Expiry = DateTime.UtcNow.AddDays(7)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            var user = await _mongoDbService.GetUserByEmailAsync(request.Email.ToLower().Trim());
            if (user == null)
                return Unauthorized(new ErrorResponse { Message = "Invalid email or password" });

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new ErrorResponse { Message = "Invalid email or password" });

            user.UpdatedAt = DateTime.UtcNow;
            await _mongoDbService.UpdateUserAsync(user.Id!, user);

            var token = _jwtHelper.GenerateToken(user);
            var response = new AuthResponseDto
            {
                Token = token,
                UserId = user.Id!,
                Email = user.Email,
                FullName = user.FullName,
                Expiry = DateTime.UtcNow.AddDays(7)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ErrorResponse { Message = "User not authenticated" });

            var user = await _mongoDbService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new ErrorResponse { Message = "User not found" });

            var profile = new UserProfileDto
            {
                Id = user.Id!,
                Email = user.Email,
                FullName = user.FullName,
                Headline = user.Headline,
                Bio = user.Bio,
                Location = user.Location,
                AvatarUrl = user.AvatarUrl,
                Skills = user.Skills,
                Interests = user.Interests,
                Availability = user.Availability,
                LookingFor = user.LookingFor,
                GithubUrl = user.GithubUrl,
                LinkedinUrl = user.LinkedinUrl,
                PortfolioUrl = user.PortfolioUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
        }
    }
}