using CoFounderFinder.Api.Models;
using CoFounderFinder.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CoFounderFinder.Api.DTOs;  // Add this for response DTOs

namespace CoFounderFinder.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly MongoDbService _mongoDbService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(MongoDbService mongoDbService, ILogger<MessagesController> logger)
    {
        _mongoDbService = mongoDbService;
        _logger = logger;
    }

    [HttpGet("match/{matchId}")]
    [ProducesResponseType(typeof(List<MessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessagesForMatch(string matchId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            
            // Verify user is part of this match
            var matches = await _mongoDbService.GetMatchesForUserAsync(userId);
            var match = matches.FirstOrDefault(m => m.Id == matchId);
            
            if (match == null)
            {
                return Forbid();
            }

            var messages = await _mongoDbService.GetMessagesForMatchAsync(matchId);
            
            // Mark messages as read
            await _mongoDbService.MarkMessagesAsReadAsync(matchId, userId);

            var messageResponses = messages.Select(m => new MessageResponse
            {
                Id = m.Id!,
                SenderId = m.SenderId,
                Content = m.Content,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt,
                IsMine = m.SenderId == userId
            }).ToList();

            return Ok(messageResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for match: {MatchId}", matchId);
            return StatusCode(500, new { message = "An error occurred retrieving messages" });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { message = "Message content cannot be empty" });
            }
            
            // Verify user is part of this match
            var matches = await _mongoDbService.GetMatchesForUserAsync(senderId);
            var match = matches.FirstOrDefault(m => m.Id == request.MatchId);
            
            if (match == null)
            {
                return Forbid();
            }

            // Only allow messaging if match is accepted
            if (match.Status != "accepted")
            {
                return BadRequest(new { message = "You can only message accepted matches" });
            }

            var receiverId = match.User1Id == senderId ? match.User2Id : match.User1Id;

            var message = new Message
            {
                MatchId = request.MatchId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = request.Content.Trim(),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _mongoDbService.CreateMessageAsync(message);
            _logger.LogInformation("Message sent from {SenderId} to {ReceiverId}", senderId, receiverId);

            var response = new MessageResponse
            {
                Id = message.Id!,
                SenderId = message.SenderId,
                Content = message.Content,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt,
                IsMine = true
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, new { message = "An error occurred sending message" });
        }
    }

    [HttpGet("unread/count")]
    [ProducesResponseType(typeof(UnreadCountResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var matches = await _mongoDbService.GetMatchesForUserAsync(userId);
            var acceptedMatchIds = matches.Where(m => m.Status == "accepted").Select(m => m.Id!).ToList();
            
            int unreadCount = 0;
            foreach (var matchId in acceptedMatchIds)
            {
                var messages = await _mongoDbService.GetMessagesForMatchAsync(matchId);
                unreadCount += messages.Count(m => m.ReceiverId == userId && !m.IsRead);
            }

            return Ok(new UnreadCountResponse { Count = unreadCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}