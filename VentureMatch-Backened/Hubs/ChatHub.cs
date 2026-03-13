using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using CoFounderFinder.Api.Services;
using CoFounderFinder.Api.Models;
using System.Security.Claims;

namespace CoFounderFinder.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly MongoDbService _mongoDbService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(MongoDbService mongoDbService, ILogger<ChatHub> logger)
    {
        _mongoDbService = mongoDbService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            _logger.LogInformation("User {UserId} connected to chat", userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
            _logger.LogInformation("User {UserId} disconnected from chat", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinMatchRoom(string matchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, matchId);
        _logger.LogInformation("User joined match room: {MatchId}", matchId);
    }

    public async Task LeaveMatchRoom(string matchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, matchId);
    }

    public async Task SendMessage(string matchId, string receiverId, string content)
    {
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(senderId))
        {
            throw new HubException("User not authenticated");
        }

        // Save to database
        var message = new Message
        {
            MatchId = matchId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _mongoDbService.CreateMessageAsync(message);

        // Send to receiver
        await Clients.Group(matchId).SendAsync("ReceiveMessage", new
        {
            id = message.Id,
            senderId = message.SenderId,
            content = message.Content,
            createdAt = message.CreatedAt,
            isMine = false
        });

        // Notify receiver about unread message
        await Clients.Group($"user-{receiverId}").SendAsync("NewMessage", new
        {
            matchId,
            messageId = message.Id,
            senderId
        });

        _logger.LogInformation("Message sent in match {MatchId}", matchId);
    }

    public async Task MarkAsRead(string matchId, string messageId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
            return;

        await _mongoDbService.MarkMessageAsReadAsync(messageId);
        
        // Notify sender that message was read
        await Clients.Group(matchId).SendAsync("MessageRead", new
        {
            messageId,
            readBy = userId
        });
    }

    public async Task Typing(string matchId, bool isTyping)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
            return;

        await Clients.Group(matchId).SendAsync("UserTyping", new
        {
            userId,
            isTyping
        });
    }
}