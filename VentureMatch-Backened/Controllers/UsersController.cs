using CoFounderFinder.Api.DTOs;
using CoFounderFinder.Api.Models;
using CoFounderFinder.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoFounderFinder.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly MongoDbService _mongoDbService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(MongoDbService mongoDbService, ILogger<UsersController> logger)
    {
        _mongoDbService = mongoDbService;
        _logger = logger;
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _mongoDbService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var profile = MapToProfileDto(user);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            return StatusCode(500, new { message = "An error occurred retrieving profile" });
        }
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _mongoDbService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Update fields
            user.FullName = request.FullName ?? user.FullName;
            user.Headline = request.Headline ?? user.Headline;
            user.Bio = request.Bio ?? user.Bio;
            user.Location = request.Location ?? user.Location;
            user.AvatarUrl = request.AvatarUrl ?? user.AvatarUrl;
            user.Skills = request.Skills ?? user.Skills;
            user.Interests = request.Interests ?? user.Interests;
            user.Availability = request.Availability ?? user.Availability;
            user.LookingFor = request.LookingFor ?? user.LookingFor;
            user.UpdatedAt = DateTime.UtcNow;

            await _mongoDbService.UpdateUserAsync(user.Id!, user);
            _logger.LogInformation("Profile updated for user: {UserId}", userId);

            var profile = MapToProfileDto(user);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user");
            return StatusCode(500, new { message = "An error occurred updating profile" });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _mongoDbService.GetAllUsersAsync();
            var profiles = users.Select(u => MapToProfileDto(u)).ToList();
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, new { message = "An error occurred retrieving users" });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(string id)
    {
        try
        {
            var user = await _mongoDbService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var profile = MapToProfileDto(user);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred retrieving user" });
        }
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(List<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsers([FromQuery] string? skill, [FromQuery] string? location)
    {
        try
        {
            var users = await _mongoDbService.GetAllUsersAsync();
            
            if (!string.IsNullOrEmpty(skill))
            {
                users = users.Where(u => u.Skills.Any(s => 
                    s.Contains(skill, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            if (!string.IsNullOrEmpty(location))
            {
                users = users.Where(u => 
                    u.Location.Contains(location, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var profiles = users.Select(u => MapToProfileDto(u)).ToList();
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return StatusCode(500, new { message = "An error occurred searching users" });
        }
    }

    private UserProfileDto MapToProfileDto(User user)
    {
        return new UserProfileDto
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
            LookingFor = user.LookingFor
        };
    }
}