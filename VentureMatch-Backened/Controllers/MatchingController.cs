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
public class MatchingController : ControllerBase
{
    private readonly MongoDbService _mongoDbService;
    private readonly ILogger<MatchingController> _logger;

    public MatchingController(MongoDbService mongoDbService, ILogger<MatchingController> logger)
    {
        _mongoDbService = mongoDbService;
        _logger = logger;
    }

    // ==================== GET POTENTIAL MATCHES ====================
    
    [HttpGet("potential")]
    public async Task<IActionResult> GetPotentialMatches()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            _logger.LogInformation("Getting potential matches for user {UserId}", userId);

            var currentUser = await _mongoDbService.GetUserByIdAsync(userId);
            if (currentUser == null)
                return NotFound(new { message = "User not found" });

            var allUsers = await _mongoDbService.GetAllUsersAsync();
            
            // ============ FIXED FILTERING LOGIC ============
            // Get existing matches
            var existingMatches = await _mongoDbService.GetMatchesForUserAsync(userId);

            // Users that THIS USER has already LIKED (as the one who initiated the like)
            var usersILiked = existingMatches
                .Where(m => m.User1Id == userId)
                .Select(m => m.User2Id!)
                .ToHashSet();

            // Users that THIS USER has already PASSED on (as the one who initiated the pass)
            var usersIPassed = existingMatches
                .Where(m => m.User1Id == userId && m.Status == "rejected")
                .Select(m => m.User2Id!)
                .ToHashSet();

            // Users that are already ACCEPTED matches (mutual)
            var acceptedMatches = existingMatches
                .Where(m => m.Status == "accepted")
                .Select(m => m.User1Id == userId ? m.User2Id! : m.User1Id!)
                .ToHashSet();

            // IMPORTANT: ONLY filter out users that THIS USER has already acted UPON
            // Do NOT filter out users who liked THIS USER (they should still appear!)
            var usersToFilter = usersILiked
                .Union(usersIPassed)
                .Union(acceptedMatches)
                .ToHashSet();

            // Log for debugging
            _logger.LogInformation("Total matches: {Count}", existingMatches.Count);
            _logger.LogInformation("Users I liked: {Count}", usersILiked.Count);
            _logger.LogInformation("Users I passed: {Count}", usersIPassed.Count);
            _logger.LogInformation("Accepted matches: {Count}", acceptedMatches.Count);
            _logger.LogInformation("Users to filter: {Count}", usersToFilter.Count);
            // ============ END FIXED FILTERING ============

            var matches = new List<PotentialMatchResponse>();
            var remainingUsers = new List<User>();

            foreach (var user in allUsers.Where(u => u.Id != userId))
            {
                // Skip if user should be filtered (users THIS USER has already acted on)
                if (usersToFilter.Contains(user.Id ?? string.Empty))
                {
                    _logger.LogDebug("Filtering out user {UserId} - already acted upon", user.Id);
                    continue;
                }
                
                remainingUsers.Add(user);
                
                var score = CalculateMatchScore(currentUser, user);
                var reason = GetMatchReason(currentUser, user, score);
                
                // Regular matches with score >= 15 (lowered threshold)
                if (score >= 15)
                {
                    matches.Add(new PotentialMatchResponse
                    {
                        User = new UserSummaryDto
                        {
                            Id = user.Id ?? string.Empty,
                            FullName = user.FullName ?? "",
                            Headline = user.Headline ?? "",
                            Bio = user.Bio ?? "",
                            Location = user.Location ?? "",
                            AvatarUrl = user.AvatarUrl,
                            Skills = user.Skills ?? new List<string>(),
                            Interests = user.Interests ?? new List<string>(),
                            Availability = user.Availability ?? "Not specified"
                        },
                        MatchScore = score,
                        MatchReason = reason
                    });
                }
            }

            // Get lenient matches (users with lower scores) from remaining users
            var lenientMatches = await GetLenientMatchesList(currentUser, remainingUsers);

            // Add lenient matches that aren't already in regular matches
            foreach (var lenientMatch in lenientMatches)
            {
                if (!matches.Any(m => m.User.Id == lenientMatch.User.Id))
                {
                    matches.Add(lenientMatch);
                }
            }

            return Ok(matches.OrderByDescending(m => m.MatchScore));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting potential matches");
            return StatusCode(500, new { message = "An error occurred while fetching matches" });
        }
    }

    // ==================== LIKE USER ====================
    
    [HttpPost("like/{targetUserId}")]
    public async Task<IActionResult> LikeUser(string targetUserId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            _logger.LogInformation("User {UserId} liking user {TargetUserId}", userId, targetUserId);

            // Check if users exist
            var currentUser = await _mongoDbService.GetUserByIdAsync(userId);
            var targetUser = await _mongoDbService.GetUserByIdAsync(targetUserId);
            
            if (currentUser == null || targetUser == null)
                return NotFound(new { message = "User not found" });

            // Check if a match already exists between these users
            var existingMatch = await _mongoDbService.GetMatchBetweenUsersAsync(userId, targetUserId);
            
            if (existingMatch != null)
            {
                _logger.LogInformation("Existing match found with status: {Status}", existingMatch.Status);
                
                // If match exists and is pending
                if (existingMatch.Status == "pending")
                {
                    // Check if this is a mutual like (the other user already liked this user)
                    if (existingMatch.User1Id == targetUserId)
                    {
                        // Target user already liked current user - this is a match!
                        existingMatch.Status = "accepted";
                        existingMatch.UpdatedAt = DateTime.UtcNow;
                        await _mongoDbService.UpdateMatchAsync(existingMatch);
                        
                        _logger.LogInformation("MATCH! Users {UserId1} and {UserId2} are now matched", userId, targetUserId);
                        
                        return Ok(new 
                        { 
                            isMatch = true, 
                            matchId = existingMatch.Id,
                            message = "It's a match! 🎉" 
                        });
                    }
                    else
                    {
                        // Current user already liked target user (duplicate like)
                        return Ok(new 
                        { 
                            isMatch = false, 
                            message = "You have already liked this user" 
                        });
                    }
                }
                else if (existingMatch.Status == "accepted")
                {
                    return Ok(new 
                    { 
                        isMatch = true, 
                        matchId = existingMatch.Id,
                        message = "Already matched!" 
                    });
                }
                else
                {
                    return Ok(new 
                    { 
                        isMatch = false, 
                        message = "You have already interacted with this user" 
                    });
                }
            }

            // Before creating a new match, check if the OTHER user already liked THIS user
            var reverseMatch = await _mongoDbService.GetMatchBetweenUsersAsync(targetUserId, userId);
            
            if (reverseMatch != null && reverseMatch.Status == "pending")
            {
                // Mutual like detected! Update the existing match to accepted
                reverseMatch.Status = "accepted";
                reverseMatch.UpdatedAt = DateTime.UtcNow;
                await _mongoDbService.UpdateMatchAsync(reverseMatch);
                
                _logger.LogInformation("MUTUAL MATCH DETECTED! Users {UserId1} and {UserId2} are now matched", userId, targetUserId);
                
                return Ok(new 
                { 
                    isMatch = true, 
                    matchId = reverseMatch.Id,
                    message = "It's a match! 🎉" 
                });
            }

            // Calculate match score for this pair
            var matchScore = CalculateMatchScore(currentUser, targetUser);

            // Create new match record (CURRENT USER LIKES TARGET USER)
            var match = new Match
            {
                User1Id = userId,
                User2Id = targetUserId,
                Status = "pending",
                MatchScore = matchScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _mongoDbService.CreateMatchAsync(match);
            _logger.LogInformation("User {UserId} liked user {TargetUserId} - Match created with ID: {MatchId}", 
                userId, targetUserId, match.Id);

            return Ok(new 
            { 
                isMatch = false, 
                matchId = match.Id,
                message = "User liked successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error liking user {TargetUserId}", targetUserId);
            return StatusCode(500, new { message = "An error occurred while processing your like" });
        }
    }

    // ==================== PASS USER ====================
    
    [HttpPost("pass/{targetUserId}")]
    public async Task<IActionResult> PassUser(string targetUserId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            _logger.LogInformation("User {UserId} passing on user {TargetUserId}", userId, targetUserId);

            // Check if a match already exists
            var existingMatch = await _mongoDbService.GetMatchBetweenUsersAsync(userId, targetUserId);
            
            if (existingMatch != null)
            {
                // Update existing match to rejected
                existingMatch.Status = "rejected";
                existingMatch.UpdatedAt = DateTime.UtcNow;
                await _mongoDbService.UpdateMatchAsync(existingMatch);
                
                _logger.LogInformation("Updated existing match {MatchId} to rejected", existingMatch.Id);
            }
            else
            {
                // Create new rejected match
                var match = new Match
                {
                    User1Id = userId,
                    User2Id = targetUserId,
                    Status = "rejected",
                    MatchScore = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _mongoDbService.CreateMatchAsync(match);
                
                _logger.LogInformation("Created new rejected match between {UserId} and {TargetUserId}", 
                    userId, targetUserId);
            }

            return Ok(new { message = "User passed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error passing user {TargetUserId}", targetUserId);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    // ==================== GET MY MATCHES ====================
    
    [HttpGet("matches")]
    public async Task<IActionResult> GetMyMatches()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            _logger.LogInformation("Getting matches for user {UserId}", userId);

            var matches = await _mongoDbService.GetMatchesForUserAsync(userId);
            var acceptedMatches = matches.Where(m => m.Status == "accepted").ToList();
            
            var result = new List<object>();
            
            foreach (var match in acceptedMatches)
            {
                var otherUserId = match.User1Id == userId ? match.User2Id : match.User1Id;
                var user = await _mongoDbService.GetUserByIdAsync(otherUserId);
                
                if (user != null)
                {
                    result.Add(new
                    {
                        matchId = match.Id,
                        user = new
                        {
                            Id = user.Id ?? string.Empty,
                            FullName = user.FullName ?? "",
                            Headline = user.Headline ?? "",
                            AvatarUrl = user.AvatarUrl
                        },
                        matchedAt = match.CreatedAt,
                        matchScore = match.MatchScore
                    });
                }
            }

            _logger.LogInformation("Found {Count} matches for user {UserId}", result.Count, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting matches for user");
            return StatusCode(500, new { message = "An error occurred while fetching matches" });
        }
    }

    // ==================== LENIENT MATCHING HELPER ====================
    
    private async Task<List<PotentialMatchResponse>> GetLenientMatchesList(User currentUser, List<User> otherUsers)
    {
        var matches = new List<PotentialMatchResponse>();

        foreach (var user in otherUsers)
        {
            var score = CalculateLenientMatchScore(currentUser, user);
            
            if (score > 0)
            {
                matches.Add(new PotentialMatchResponse
                {
                    User = new UserSummaryDto
                    {
                        Id = user.Id ?? string.Empty,
                        FullName = user.FullName ?? "Unknown",
                        Headline = user.Headline ?? "No headline",
                        Bio = user.Bio ?? "",
                        Location = user.Location ?? "Location not specified",
                        AvatarUrl = user.AvatarUrl,
                        Skills = user.Skills ?? new List<string>(),
                        Interests = user.Interests ?? new List<string>(),
                        Availability = user.Availability ?? "Not specified"
                    },
                    MatchScore = score,
                    MatchReason = GetLenientMatchReason(currentUser, user)
                });
            }
        }

        return matches.OrderByDescending(m => m.MatchScore).ToList();
    }

    // ==================== MATCH SCORE CALCULATIONS ====================
    
    private int CalculateMatchScore(User user1, User user2)
    {
        int score = 0;
        int maxScore = 100;

        // 1. Common Skills (30 points)
        var commonSkills = user1.Skills?.Intersect(user2.Skills ?? new List<string>(), StringComparer.OrdinalIgnoreCase).Count() ?? 0;
        score += commonSkills * 10; // 10 points per common skill (max 30)

        // 2. Common Interests (20 points)
        var commonInterests = user1.Interests?.Intersect(user2.Interests ?? new List<string>(), StringComparer.OrdinalIgnoreCase).Count() ?? 0;
        score += commonInterests * 7; // 7 points per common interest (max 20)

        // 3. Complementary Skills (30 points) - You have what they need
        var user1Needs = user1.LookingFor?.Select(lf => lf.ToLower()) ?? new List<string>();
        var user2Has = user2.Skills?.Select(s => s.ToLower()) ?? new List<string>();
        var complementary1 = user1Needs.Intersect(user2Has).Count();
        score += complementary1 * 10; // 10 points per complementary skill (max 20)

        // 4. They have what you need (20 points)
        var user2Needs = user2.LookingFor?.Select(lf => lf.ToLower()) ?? new List<string>();
        var user1Has = user1.Skills?.Select(s => s.ToLower()) ?? new List<string>();
        var complementary2 = user2Needs.Intersect(user1Has).Count();
        score += complementary2 * 10; // 10 points per complementary skill (max 20)

        // 5. Location Bonus (10 points)
        if (!string.IsNullOrEmpty(user1.Location) && !string.IsNullOrEmpty(user2.Location))
        {
            if (user1.Location.Contains(user2.Location, StringComparison.OrdinalIgnoreCase) ||
                user2.Location.Contains(user1.Location, StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
            }
        }

        // 6. Availability Match (5 points)
        if (!string.IsNullOrEmpty(user1.Availability) && !string.IsNullOrEmpty(user2.Availability) &&
            user1.Availability.Equals(user2.Availability, StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
        }

        return Math.Min(score, maxScore);
    }

    private int CalculateLenientMatchScore(User user1, User user2)
    {
        int score = 5; // Base score for everyone

        // Any skill match (even partial)
        if (user1.Skills != null && user2.Skills != null)
        {
            var anySkillMatch = user1.Skills.Any(s1 => 
                user2.Skills.Any(s2 => s2.Contains(s1, StringComparison.OrdinalIgnoreCase) || 
                                       s1.Contains(s2, StringComparison.OrdinalIgnoreCase)));
            if (anySkillMatch) score += 15;
        }

        // Any interest match
        if (user1.Interests != null && user2.Interests != null)
        {
            var anyInterestMatch = user1.Interests.Any(i1 => 
                user2.Interests.Any(i2 => i2.Contains(i1, StringComparison.OrdinalIgnoreCase) ||
                                          i1.Contains(i2, StringComparison.OrdinalIgnoreCase)));
            if (anyInterestMatch) score += 10;
        }

        // Same location (even partial)
        if (!string.IsNullOrEmpty(user1.Location) && !string.IsNullOrEmpty(user2.Location))
        {
            if (user1.Location.Contains(user2.Location, StringComparison.OrdinalIgnoreCase) ||
                user2.Location.Contains(user1.Location, StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
            }
        }

        // Both have skills (even if different)
        if ((user1.Skills?.Any() ?? false) && (user2.Skills?.Any() ?? false))
            score += 5;

        // Both have interests
        if ((user1.Interests?.Any() ?? false) && (user2.Interests?.Any() ?? false))
            score += 5;

        return Math.Min(score, 50);
    }

    // ==================== MATCH REASONS ====================
    
    private string GetMatchReason(User user1, User user2, int score)
    {
        var reasons = new List<string>();

        // Check for common skills
        var commonSkills = user1.Skills?.Intersect(user2.Skills ?? new List<string>(), StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        if (commonSkills.Any())
        {
            reasons.Add($"Share skills: {string.Join(", ", commonSkills.Take(2))}");
        }

        // Check for complementary skills
        var user1Needs = user1.LookingFor?.Select(lf => lf.ToLower()) ?? new List<string>();
        var user2Has = user2.Skills?.Select(s => s.ToLower()) ?? new List<string>();
        var complementary = user1Needs.Intersect(user2Has).ToList();
        
        if (complementary.Any())
        {
            reasons.Add($"You need {complementary.First()}, they have it");
        }
        else
        {
            var user2Needs = user2.LookingFor?.Select(lf => lf.ToLower()) ?? new List<string>();
            var user1Has = user1.Skills?.Select(s => s.ToLower()) ?? new List<string>();
            var theyNeed = user2Needs.Intersect(user1Has).ToList();
            
            if (theyNeed.Any())
            {
                reasons.Add($"They need {theyNeed.First()}, you have it");
            }
        }

        // Check for common interests
        var commonInterests = user1.Interests?.Intersect(user2.Interests ?? new List<string>(), StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        if (commonInterests.Any())
        {
            reasons.Add($"Share interests: {string.Join(", ", commonInterests.Take(2))}");
        }

        // Location match
        if (!string.IsNullOrEmpty(user1.Location) && !string.IsNullOrEmpty(user2.Location) &&
            user1.Location.Contains(user2.Location, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add($"Both in {user2.Location}");
        }

        if (reasons.Any())
            return string.Join(" • ", reasons.Take(2));

        return score >= 30 ? "Good potential match" : "Based on your profile";
    }

    private string GetLenientMatchReason(User user1, User user2)
    {
        if (user1.Skills?.Any() == true && user2.Skills?.Any() == true)
            return "Both have technical skills";
        
        if (!string.IsNullOrEmpty(user1.Location) && !string.IsNullOrEmpty(user2.Location) &&
            user1.Location.Equals(user2.Location, StringComparison.OrdinalIgnoreCase))
            return $"Both located in {user1.Location}";
        
        if (user1.Interests?.Any() == true && user2.Interests?.Any() == true)
            return "Share similar interests";

        return "Potential connection";
    }

    // ==================== HELPER METHODS ====================
    
    private async Task<bool> MatchExistsAsync(string user1Id, string user2Id)
    {
        var match = await _mongoDbService.GetMatchBetweenUsersAsync(user1Id, user2Id);
        return match != null;
    }
}