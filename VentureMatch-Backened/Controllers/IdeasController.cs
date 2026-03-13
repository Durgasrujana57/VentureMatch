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
public class IdeasController : ControllerBase
{
    private readonly MongoDbService _mongoDbService;
    private readonly ILogger<IdeasController> _logger;

    public IdeasController(MongoDbService mongoDbService, ILogger<IdeasController> logger)
    {
        _mongoDbService = mongoDbService;
        _logger = logger;
    }

    // ==================== GET ALL IDEAS ====================
    /// <summary>
    /// Retrieves all startup ideas with optional filtering
    /// </summary>
    /// <param name="industry">Filter by industry</param>
    /// <param name="stage">Filter by stage (Idea, MVP, Growth, etc.)</param>
    /// <param name="search">Search in title and description</param>
    /// <returns>List of ideas with user details</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllIdeas(
        [FromQuery] string? industry, 
        [FromQuery] string? stage, 
        [FromQuery] string? search)
    {
        try
        {
            var ideas = await _mongoDbService.SearchIdeasAsync(industry, stage, search);
            
            if (ideas == null || !ideas.Any())
            {
                return Ok(new List<IdeaResponseDto>());
            }

            var result = new List<IdeaResponseDto>();

            foreach (var idea in ideas)
            {
                var user = await _mongoDbService.GetUserByIdAsync(idea.UserId);
                result.Add(MapToIdeaResponseDto(idea, user));
            }

            _logger.LogInformation("Retrieved {Count} ideas", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ideas");
            return StatusCode(500, new { message = "An error occurred while fetching ideas" });
        }
    }

    // ==================== GET IDEA BY ID ====================
    /// <summary>
    /// Retrieves a specific idea by ID and increments view count
    /// </summary>
    /// <param name="id">Idea ID</param>
    /// <returns>Idea details with user information</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetIdeaById(string id)
    {
        try
        {
            var idea = await _mongoDbService.GetIdeaByIdAsync(id);
            if (idea == null)
                return NotFound(new { message = "Idea not found" });

            // Increment view count
            await _mongoDbService.IncrementIdeaViewsAsync(id);
            idea.Views++;

            var user = await _mongoDbService.GetUserByIdAsync(idea.UserId);
            var result = MapToIdeaResponseDto(idea, user);

            _logger.LogInformation("Retrieved idea: {IdeaId}", id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting idea by ID: {IdeaId}", id);
            return StatusCode(500, new { message = "An error occurred while fetching the idea" });
        }
    }

    // ==================== CREATE IDEA ====================
    /// <summary>
    /// Creates a new startup idea
    /// </summary>
    /// <param name="request">Idea details</param>
    /// <returns>Created idea ID</returns>
    [HttpPost]
    public async Task<IActionResult> CreateIdea([FromBody] CreateIdeaDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            var idea = new StartupIdea
            {
                UserId = userId,
                Title = request.Title,
                Description = request.Description,
                ProblemStatement = request.ProblemStatement ?? "",
                Solution = request.Solution ?? "",
                Industry = request.Industry ?? "",
                Stage = request.Stage ?? "Idea",
                RolesNeeded = request.RolesNeeded?.Select(r => new RoleNeeded
                {
                    Role = r.Role,
                    Skills = r.Skills,
                    Commitment = r.Commitment
                }).ToList() ?? new List<RoleNeeded>(),
                LookingForSkills = request.LookingForSkills ?? new List<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Views = 0,
                Applications = 0
            };

            await _mongoDbService.CreateIdeaAsync(idea);
            _logger.LogInformation("User {UserId} created idea: {IdeaId}", userId, idea.Id);

            return Ok(new { 
                message = "Idea created successfully", 
                ideaId = idea.Id 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating idea");
            return StatusCode(500, new { message = "An error occurred while creating the idea" });
        }
    }

    // ==================== UPDATE IDEA ====================
    /// <summary>
    /// Updates an existing idea (owner only)
    /// </summary>
    /// <param name="id">Idea ID</param>
    /// <param name="request">Updated fields</param>
    /// <returns>Success message</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIdea(string id, [FromBody] UpdateIdeaDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            var existingIdea = await _mongoDbService.GetIdeaByIdAsync(id);
            if (existingIdea == null)
                return NotFound(new { message = "Idea not found" });

            // Check if user owns the idea
            if (existingIdea.UserId != userId)
                return Forbid();

            // Update fields
            if (!string.IsNullOrEmpty(request.Title))
                existingIdea.Title = request.Title;
            
            if (!string.IsNullOrEmpty(request.Description))
                existingIdea.Description = request.Description;
            
            if (!string.IsNullOrEmpty(request.ProblemStatement))
                existingIdea.ProblemStatement = request.ProblemStatement;
            
            if (!string.IsNullOrEmpty(request.Solution))
                existingIdea.Solution = request.Solution;
            
            if (!string.IsNullOrEmpty(request.Industry))
                existingIdea.Industry = request.Industry;
            
            if (!string.IsNullOrEmpty(request.Stage))
                existingIdea.Stage = request.Stage;
            
            if (request.RolesNeeded != null)
            {
                existingIdea.RolesNeeded = request.RolesNeeded.Select(r => new RoleNeeded
                {
                    Role = r.Role,
                    Skills = r.Skills,
                    Commitment = r.Commitment
                }).ToList();
            }
            
            if (request.LookingForSkills != null)
                existingIdea.LookingForSkills = request.LookingForSkills;

            existingIdea.UpdatedAt = DateTime.UtcNow;

            await _mongoDbService.UpdateIdeaAsync(id, existingIdea);
            _logger.LogInformation("User {UserId} updated idea: {IdeaId}", userId, id);

            return Ok(new { message = "Idea updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating idea: {IdeaId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the idea" });
        }
    }

    // ==================== DELETE IDEA ====================
    /// <summary>
    /// Deletes an idea (owner only)
    /// </summary>
    /// <param name="id">Idea ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIdea(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            var existingIdea = await _mongoDbService.GetIdeaByIdAsync(id);
            if (existingIdea == null)
                return NotFound(new { message = "Idea not found" });

            // Check if user owns the idea
            if (existingIdea.UserId != userId)
                return Forbid();

            await _mongoDbService.DeleteIdeaAsync(id);
            _logger.LogInformation("User {UserId} deleted idea: {IdeaId}", userId, id);

            return Ok(new { message = "Idea deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting idea: {IdeaId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the idea" });
        }
    }

    // ==================== GET USER'S IDEAS ====================
    /// <summary>
    /// Retrieves all ideas created by a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of ideas with user details</returns>
    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserIdeas(string userId)
    {
        try
        {
            var ideas = await _mongoDbService.GetIdeasByUserIdAsync(userId);
            
            if (ideas == null || !ideas.Any())
            {
                return Ok(new List<IdeaResponseDto>());
            }

            var user = await _mongoDbService.GetUserByIdAsync(userId);
            var result = ideas.Select(idea => MapToIdeaResponseDto(idea, user)).ToList();

            _logger.LogInformation("Retrieved {Count} ideas for user: {UserId}", result.Count, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ideas for user: {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while fetching user ideas" });
        }
    }

    // ==================== CHECK MY APPLICATION STATUS ====================
    /// <summary>
    /// Allows a user to check their own application status for a specific idea
    /// </summary>
    /// <param name="ideaId">Idea ID</param>
    /// <returns>User's application if exists</returns>
    [HttpGet("{ideaId}/my-application")]
    public async Task<IActionResult> GetMyApplication(string ideaId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            _logger.LogInformation("User {UserId} checking their application for idea: {IdeaId}", userId, ideaId);

            // Get all applications by this user
            var userApplications = await _mongoDbService.GetApplicationsByUserAsync(userId);
            
            // Find the one for this specific idea
            var myApplication = userApplications.FirstOrDefault(a => a.IdeaId == ideaId);
            
            if (myApplication == null)
            {
                _logger.LogDebug("No application found for user {UserId} on idea {IdeaId}", userId, ideaId);
                return Ok(null);
            }

            // Get the idea details
            var idea = await _mongoDbService.GetIdeaByIdAsync(ideaId);
            
            // Get the user details (the applicant)
            var applicantUser = await _mongoDbService.GetUserByIdAsync(myApplication.UserId);
            
            var result = MapToApplicationResponseDto(myApplication, applicantUser, idea);
            
            _logger.LogInformation("Found application for user {UserId} on idea {IdeaId} with status: {Status}", 
                userId, ideaId, myApplication.Status);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user's application for idea: {IdeaId}", ideaId);
            return StatusCode(500, new { message = "An error occurred while checking application status" });
        }
    }

    // ==================== APPLY TO IDEA ====================
    /// <summary>
    /// Submits an application to join an idea team
    /// </summary>
    /// <param name="request">Application details</param>
    /// <returns>Application ID</returns>
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyToIdea([FromBody] ApplyToIdeaDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            var idea = await _mongoDbService.GetIdeaByIdAsync(request.IdeaId);
            if (idea == null)
                return NotFound(new { message = "Idea not found" });

            // Check if user is applying to their own idea
            if (idea.UserId == userId)
                return BadRequest(new { message = "You cannot apply to your own idea" });

            // Check if user already applied
            var existingApplications = await _mongoDbService.GetApplicationsByUserAsync(userId);
            if (existingApplications.Any(a => a.IdeaId == request.IdeaId))
            {
                return BadRequest(new { message = "You have already applied to this idea" });
            }

            var application = new IdeaApplication
            {
                IdeaId = request.IdeaId,
                UserId = userId,
                Message = request.Message ?? "",
                Status = "pending",
                AppliedAt = DateTime.UtcNow
            };

            await _mongoDbService.CreateApplicationAsync(application);
            
            // Increment applications count on idea
            await _mongoDbService.IncrementIdeaApplicationsAsync(request.IdeaId);
            
            _logger.LogInformation("User {UserId} applied to idea: {IdeaId}", userId, request.IdeaId);

            return Ok(new { 
                message = "Application submitted successfully", 
                applicationId = application.Id 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying to idea");
            return StatusCode(500, new { message = "An error occurred while applying to the idea" });
        }
    }

    // ==================== GET APPLICATIONS FOR IDEA ====================
    /// <summary>
    /// Gets all applications for a specific idea (owner only)
    /// </summary>
    /// <param name="ideaId">Idea ID</param>
    /// <returns>List of applications with applicant details</returns>
    [HttpGet("{ideaId}/applications")]
    public async Task<IActionResult> GetIdeaApplications(string ideaId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            var idea = await _mongoDbService.GetIdeaByIdAsync(ideaId);
            if (idea == null)
                return NotFound(new { message = "Idea not found" });

            // Only idea owner can see applications
            if (idea.UserId != userId)
                return Forbid();

            var applications = await _mongoDbService.GetApplicationsForIdeaAsync(ideaId);
            
            if (applications == null || !applications.Any())
            {
                return Ok(new List<ApplicationResponseDto>());
            }

            var result = new List<ApplicationResponseDto>();

            foreach (var app in applications)
            {
                var user = await _mongoDbService.GetUserByIdAsync(app.UserId);
                result.Add(MapToApplicationResponseDto(app, user, idea));
            }

            _logger.LogInformation("Retrieved {Count} applications for idea: {IdeaId}", result.Count, ideaId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applications for idea: {IdeaId}", ideaId);
            return StatusCode(500, new { message = "An error occurred while fetching applications" });
        }
    }

    // ==================== GET APPLICATIONS BY USER ID ====================
    /// <summary>
    /// Gets all applications submitted by a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of applications with idea details</returns>
    [HttpGet("applications/user/{userId}")]
    public async Task<IActionResult> GetUserApplications(string userId)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Authorization: Users can only view their own applications
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "User not authenticated" });
                
            if (currentUserId != userId)
                return Forbid();

            var applications = await _mongoDbService.GetApplicationsByUserAsync(userId);
            
            if (applications == null || !applications.Any())
            {
                return Ok(new List<ApplicationResponseDto>());
            }

            var result = new List<ApplicationResponseDto>();

            foreach (var app in applications)
            {
                // Fetch related data in parallel for better performance
                var ideaTask = _mongoDbService.GetIdeaByIdAsync(app.IdeaId);
                var userTask = _mongoDbService.GetUserByIdAsync(app.UserId);
                
                await Task.WhenAll(ideaTask, userTask);
                
                var idea = await ideaTask;
                var user = await userTask;
                
                result.Add(MapToApplicationResponseDto(app, user, idea));
            }

            _logger.LogInformation("Retrieved {Count} applications for user: {UserId}", result.Count, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applications for user: {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while fetching user applications" });
        }
    }

    // ==================== UPDATE APPLICATION STATUS ====================
    /// <summary>
    /// Updates the status of an application (idea owner only)
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="statusRequest">New status (pending, accepted, rejected)</param>
    /// <returns>Success message</returns>
    [HttpPut("applications/{applicationId}")]
    public async Task<IActionResult> UpdateApplicationStatus(string applicationId, [FromBody] UpdateApplicationStatusDto statusRequest)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            // Get application details
            var application = await _mongoDbService.GetApplicationByIdAsync(applicationId);
            if (application == null)
                return NotFound(new { message = "Application not found" });

            // Get the idea to check ownership
            var idea = await _mongoDbService.GetIdeaByIdAsync(application.IdeaId);
            if (idea == null)
                return NotFound(new { message = "Associated idea not found" });

            // Only idea owner can update application status
            if (idea.UserId != userId)
                return Forbid();

            // Validate status
            var validStatuses = new[] { "pending", "accepted", "rejected" };
            if (!validStatuses.Contains(statusRequest.Status.ToLower()))
            {
                return BadRequest(new { message = "Status must be: pending, accepted, or rejected" });
            }

            await _mongoDbService.UpdateApplicationStatusAsync(applicationId, statusRequest.Status);
            
            _logger.LogInformation("Application {ApplicationId} status updated to: {Status} by user {UserId}", 
                applicationId, statusRequest.Status, userId);

            return Ok(new { message = "Application status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application status: {ApplicationId}", applicationId);
            return StatusCode(500, new { message = "An error occurred while updating application status" });
        }
    }

    // ==================== PRIVATE HELPER METHODS ====================
    
    private IdeaResponseDto MapToIdeaResponseDto(StartupIdea idea, User? user)
    {
        return new IdeaResponseDto
        {
            Id = idea.Id!,
            UserId = idea.UserId,
            Title = idea.Title,
            Description = idea.Description,
            ProblemStatement = idea.ProblemStatement,
            Solution = idea.Solution,
            Industry = idea.Industry,
            Stage = idea.Stage,
            RolesNeeded = idea.RolesNeeded?.Select(r => new RoleNeededDto
            {
                Role = r.Role,
                Skills = r.Skills,
                Commitment = r.Commitment
            }).ToList() ?? new List<RoleNeededDto>(),
            LookingForSkills = idea.LookingForSkills ?? new List<string>(),
            Views = idea.Views,
            Applications = idea.Applications,
            CreatedAt = idea.CreatedAt,
            User = user != null ? new UserSummaryDto
            {
                Id = user.Id!,
                FullName = user.FullName,
                Headline = user.Headline ?? "",
                AvatarUrl = user.AvatarUrl
            } : null
        };
    }

    private ApplicationResponseDto MapToApplicationResponseDto(IdeaApplication app, User? user, StartupIdea? idea)
    {
        return new ApplicationResponseDto
        {
            Id = app.Id!,
            IdeaId = app.IdeaId,
            UserId = app.UserId,
            Message = app.Message,
            Status = app.Status,
            AppliedAt = app.AppliedAt,
            User = user != null ? new UserSummaryDto
            {
                Id = user.Id!,
                FullName = user.FullName,
                Headline = user.Headline ?? "",
                AvatarUrl = user.AvatarUrl
            } : null,
            Idea = idea != null ? new IdeaSummaryDto
            {
                Id = idea.Id!,
                Title = idea.Title,
                Industry = idea.Industry ?? "",
                Stage = idea.Stage ?? ""
                // REMOVED: Description property - it doesn't exist in IdeaSummaryDto
            } : null
        };
    }
}