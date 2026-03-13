using CoFounderFinder.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;

namespace CoFounderFinder.Api.Services;

public class MongoDbService
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<StartupIdea> _ideas;
    private readonly IMongoCollection<Match> _matches;
    private readonly IMongoCollection<Message> _messages;
    private readonly IMongoCollection<IdeaApplication> _applications;
    private readonly ILogger<MongoDbService> _logger;

    public MongoDbService(IOptions<MongoDbSettings> settings, ILogger<MongoDbService> logger)
    {
        _logger = logger;
        
        try
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            
            _users = database.GetCollection<User>("Users");
            _ideas = database.GetCollection<StartupIdea>("StartupIdeas");
            _matches = database.GetCollection<Match>("Matches");
            _messages = database.GetCollection<Message>("Messages");
            _applications = database.GetCollection<IdeaApplication>("IdeaApplications");
            
            CreateIndexes();
            
            _logger.LogInformation("MongoDB connected successfully to database: {DatabaseName}", 
                settings.Value.DatabaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MongoDB");
            throw;
        }
    }

    private void CreateIndexes()
    {
        try
        {
            // Email unique index
            var emailIndexOptions = new CreateIndexOptions { Unique = true };
            var emailIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var emailIndexModel = new CreateIndexModel<User>(emailIndexKeys, emailIndexOptions);
            _users.Indexes.CreateOne(emailIndexModel);
            
            // Skills index
            var skillsIndexKeys = Builders<User>.IndexKeys.Ascending("Skills");
            var skillsIndexModel = new CreateIndexModel<User>(skillsIndexKeys);
            _users.Indexes.CreateOne(skillsIndexModel);
            
            // Location index
            var locationIndexKeys = Builders<User>.IndexKeys.Ascending("Location");
            var locationIndexModel = new CreateIndexModel<User>(locationIndexKeys);
            _users.Indexes.CreateOne(locationIndexModel);
            
            // Match indexes
            var user1IdIndexKeys = Builders<Match>.IndexKeys.Ascending(m => m.User1Id);
            var user1IdIndexModel = new CreateIndexModel<Match>(user1IdIndexKeys);
            _matches.Indexes.CreateOne(user1IdIndexModel);
            
            var user2IdIndexKeys = Builders<Match>.IndexKeys.Ascending(m => m.User2Id);
            var user2IdIndexModel = new CreateIndexModel<Match>(user2IdIndexKeys);
            _matches.Indexes.CreateOne(user2IdIndexModel);
            
            // Status index
            var statusIndexKeys = Builders<Match>.IndexKeys.Ascending(m => m.Status);
            var statusIndexModel = new CreateIndexModel<Match>(statusIndexKeys);
            _matches.Indexes.CreateOne(statusIndexModel);
            
            // Message indexes
            var matchIdIndexKeys = Builders<Message>.IndexKeys.Ascending(m => m.MatchId);
            var matchIdIndexModel = new CreateIndexModel<Message>(matchIdIndexKeys);
            _messages.Indexes.CreateOne(matchIdIndexModel);
            
            var receiverIdIndexKeys = Builders<Message>.IndexKeys.Ascending(m => m.ReceiverId);
            var receiverIdIndexModel = new CreateIndexModel<Message>(receiverIdIndexKeys);
            _messages.Indexes.CreateOne(receiverIdIndexModel);
            
            // Application indexes
            var ideaIdIndexKeys = Builders<IdeaApplication>.IndexKeys.Ascending(a => a.IdeaId);
            var ideaIdIndexModel = new CreateIndexModel<IdeaApplication>(ideaIdIndexKeys);
            _applications.Indexes.CreateOne(ideaIdIndexModel);
            
            var userIdAppIndexKeys = Builders<IdeaApplication>.IndexKeys.Ascending(a => a.UserId);
            var userIdAppIndexModel = new CreateIndexModel<IdeaApplication>(userIdAppIndexKeys);
            _applications.Indexes.CreateOne(userIdAppIndexModel);
            
            var statusAppIndexKeys = Builders<IdeaApplication>.IndexKeys.Ascending(a => a.Status);
            var statusAppIndexModel = new CreateIndexModel<IdeaApplication>(statusAppIndexKeys);
            _applications.Indexes.CreateOne(statusAppIndexModel);
            
            _logger.LogInformation("Database indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error creating indexes (may already exist)");
        }
    }

    // ==================== USER OPERATIONS ====================

    public async Task<User?> GetUserByIdAsync(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out _)) return null;
            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            throw;
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            return await _users.Find(u => u.Email.ToLower() == email.ToLower()).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email: {Email}", email);
            throw;
        }
    }

    public async Task CreateUserAsync(User user)
    {
        try
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _users.InsertOneAsync(user);
            _logger.LogInformation("User created successfully: {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Email}", user.Email);
            throw;
        }
    }

    public async Task UpdateUserAsync(string id, User user)
    {
        try
        {
            user.UpdatedAt = DateTime.UtcNow;
            var result = await _users.ReplaceOneAsync(u => u.Id == id, user);
            
            if (result.ModifiedCount > 0)
                _logger.LogInformation("User {UserId} updated successfully", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", id);
            throw;
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            return await _users.Find(_ => true).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            throw;
        }
    }

    public async Task<List<User>> GetUsersBySkillsAsync(List<string> skills)
    {
        try
        {
            var filter = Builders<User>.Filter.AnyIn(u => u.Skills, skills);
            return await _users.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by skills");
            throw;
        }
    }

    public async Task<List<User>> SearchUsersAsync(string? searchTerm, string? location, List<string>? skills)
    {
        try
        {
            var filterBuilder = Builders<User>.Filter;
            var filters = new List<FilterDefinition<User>>();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchFilter = filterBuilder.Or(
                    filterBuilder.Regex(u => u.FullName, new BsonRegularExpression(searchTerm, "i")),
                    filterBuilder.Regex(u => u.Headline, new BsonRegularExpression(searchTerm, "i")),
                    filterBuilder.Regex(u => u.Bio, new BsonRegularExpression(searchTerm, "i"))
                );
                filters.Add(searchFilter);
            }

            if (!string.IsNullOrEmpty(location))
            {
                filters.Add(filterBuilder.Regex(u => u.Location, new BsonRegularExpression(location, "i")));
            }

            if (skills != null && skills.Any())
            {
                filters.Add(filterBuilder.AnyIn(u => u.Skills, skills));
            }

            var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;
            return await _users.Find(finalFilter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        try
        {
            var result = await _users.DeleteOneAsync(u => u.Id == id);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            throw;
        }
    }

    // ==================== IDEA OPERATIONS ====================

    public async Task<List<StartupIdea>> GetAllIdeasAsync()
    {
        try
        {
            return await _ideas.Find(_ => true)
                .SortByDescending(i => i.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all ideas");
            throw;
        }
    }

    public async Task<StartupIdea?> GetIdeaByIdAsync(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out _)) return null;
            return await _ideas.Find(i => i.Id == id).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting idea by ID: {IdeaId}", id);
            throw;
        }
    }

    public async Task<List<StartupIdea>> GetIdeasByUserIdAsync(string userId)
    {
        try
        {
            return await _ideas.Find(i => i.UserId == userId)
                .SortByDescending(i => i.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ideas for user: {UserId}", userId);
            throw;
        }
    }

    public async Task CreateIdeaAsync(StartupIdea idea)
    {
        try
        {
            idea.CreatedAt = DateTime.UtcNow;
            idea.UpdatedAt = DateTime.UtcNow;
            await _ideas.InsertOneAsync(idea);
            _logger.LogInformation("Startup idea created: {IdeaId}", idea.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating idea for user: {UserId}", idea.UserId);
            throw;
        }
    }

    public async Task UpdateIdeaAsync(string id, StartupIdea idea)
    {
        try
        {
            idea.UpdatedAt = DateTime.UtcNow;
            await _ideas.ReplaceOneAsync(i => i.Id == id, idea);
            _logger.LogInformation("Idea {IdeaId} updated", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating idea: {IdeaId}", id);
            throw;
        }
    }

    public async Task DeleteIdeaAsync(string id)
    {
        try
        {
            await _ideas.DeleteOneAsync(i => i.Id == id);
            _logger.LogInformation("Idea {IdeaId} deleted", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting idea: {IdeaId}", id);
            throw;
        }
    }

    public async Task<List<StartupIdea>> SearchIdeasAsync(string? industry, string? stage, string? searchTerm)
    {
        try
        {
            var filterBuilder = Builders<StartupIdea>.Filter;
            var filters = new List<FilterDefinition<StartupIdea>>();

            if (!string.IsNullOrEmpty(industry))
            {
                filters.Add(filterBuilder.Eq(i => i.Industry, industry));
            }

            if (!string.IsNullOrEmpty(stage))
            {
                filters.Add(filterBuilder.Eq(i => i.Stage, stage));
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchFilter = filterBuilder.Or(
                    filterBuilder.Regex(i => i.Title, new BsonRegularExpression(searchTerm, "i")),
                    filterBuilder.Regex(i => i.Description, new BsonRegularExpression(searchTerm, "i")),
                    filterBuilder.Regex(i => i.ProblemStatement, new BsonRegularExpression(searchTerm, "i"))
                );
                filters.Add(searchFilter);
            }

            var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;
            return await _ideas.Find(finalFilter)
                .SortByDescending(i => i.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching ideas");
            throw;
        }
    }

    public async Task IncrementIdeaViewsAsync(string ideaId)
    {
        try
        {
            var update = Builders<StartupIdea>.Update.Inc(i => i.Views, 1);
            await _ideas.UpdateOneAsync(i => i.Id == ideaId, update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing views for idea: {IdeaId}", ideaId);
            throw;
        }
    }

    public async Task IncrementIdeaApplicationsAsync(string ideaId)
    {
        try
        {
            var update = Builders<StartupIdea>.Update.Inc(i => i.Applications, 1);
            await _ideas.UpdateOneAsync(i => i.Id == ideaId, update);
            _logger.LogInformation("Incremented applications count for idea: {IdeaId}", ideaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing applications for idea: {IdeaId}", ideaId);
            throw;
        }
    }

    // ==================== MATCH OPERATIONS ====================

    public async Task CreateMatchAsync(Match match)
    {
        try
        {
            match.CreatedAt = DateTime.UtcNow;
            match.UpdatedAt = DateTime.UtcNow;
            await _matches.InsertOneAsync(match);
            _logger.LogInformation("Match created: {MatchId} - {User1} -> {User2} ({Status})", 
                match.Id, match.User1Id, match.User2Id, match.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating match");
            throw;
        }
    }

    public async Task<List<Match>> GetMatchesForUserAsync(string userId)
    {
        try
        {
            return await _matches.Find(m => m.User1Id == userId || m.User2Id == userId)
                .SortByDescending(m => m.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting matches for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<Match?> GetMatchBetweenUsersAsync(string user1Id, string user2Id)
    {
        try
        {
            if (string.IsNullOrEmpty(user1Id) || string.IsNullOrEmpty(user2Id))
                return null;

            var filter = Builders<Match>.Filter.Or(
                Builders<Match>.Filter.And(
                    Builders<Match>.Filter.Eq(m => m.User1Id, user1Id),
                    Builders<Match>.Filter.Eq(m => m.User2Id, user2Id)
                ),
                Builders<Match>.Filter.And(
                    Builders<Match>.Filter.Eq(m => m.User1Id, user2Id),
                    Builders<Match>.Filter.Eq(m => m.User2Id, user1Id)
                )
            );
            
            return await _matches.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match between users {User1Id} and {User2Id}", user1Id, user2Id);
            throw;
        }
    }

    public async Task UpdateMatchAsync(Match match)
    {
        try
        {
            if (match == null || string.IsNullOrEmpty(match.Id))
                throw new ArgumentNullException(nameof(match));

            match.UpdatedAt = DateTime.UtcNow;
            var result = await _matches.ReplaceOneAsync(m => m.Id == match.Id, match);
            
            if (result.ModifiedCount > 0)
                _logger.LogInformation("Match {MatchId} updated to status: {Status}", 
                    match.Id, match.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating match {MatchId}", match?.Id);
            throw;
        }
    }

    public async Task UpdateMatchStatusAsync(string matchId, string status)
    {
        try
        {
            var update = Builders<Match>.Update
                .Set(m => m.Status, status)
                .Set(m => m.UpdatedAt, DateTime.UtcNow);
            
            var result = await _matches.UpdateOneAsync(m => m.Id == matchId, update);
            
            if (result.ModifiedCount > 0)
                _logger.LogInformation("Match {MatchId} status updated to: {Status}", matchId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating match status: {MatchId}", matchId);
            throw;
        }
    }

    public async Task<List<Match>> GetAcceptedMatchesForUserAsync(string userId)
    {
        try
        {
            var filter = Builders<Match>.Filter.And(
                Builders<Match>.Filter.Or(
                    Builders<Match>.Filter.Eq(m => m.User1Id, userId),
                    Builders<Match>.Filter.Eq(m => m.User2Id, userId)
                ),
                Builders<Match>.Filter.Eq(m => m.Status, "accepted")
            );
            
            return await _matches.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accepted matches for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteMatchAsync(string matchId)
    {
        try
        {
            var result = await _matches.DeleteOneAsync(m => m.Id == matchId);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting match: {MatchId}", matchId);
            throw;
        }
    }

    // ==================== MESSAGE OPERATIONS ====================

    public async Task CreateMessageAsync(Message message)
    {
        try
        {
            message.CreatedAt = DateTime.UtcNow;
            await _messages.InsertOneAsync(message);
            _logger.LogInformation("Message created: {MessageId}", message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating message");
            throw;
        }
    }

    public async Task<List<Message>> GetMessagesForMatchAsync(string matchId)
    {
        try
        {
            return await _messages.Find(m => m.MatchId == matchId)
                .SortBy(m => m.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for match: {MatchId}", matchId);
            throw;
        }
    }

    public async Task<Message?> GetLastMessageForMatchAsync(string matchId)
    {
        try
        {
            return await _messages.Find(m => m.MatchId == matchId)
                .SortByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last message for match: {MatchId}", matchId);
            throw;
        }
    }

    public async Task MarkMessagesAsReadAsync(string matchId, string userId)
    {
        try
        {
            var update = Builders<Message>.Update.Set(m => m.IsRead, true);
            var result = await _messages.UpdateManyAsync(
                m => m.MatchId == matchId && m.ReceiverId == userId && !m.IsRead,
                update
            );
            
            if (result.ModifiedCount > 0)
                _logger.LogInformation("Marked {Count} messages as read for user {UserId} in match {MatchId}", 
                    result.ModifiedCount, userId, matchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking messages as read");
            throw;
        }
    }

    public async Task MarkMessageAsReadAsync(string messageId)
    {
        try
        {
            var update = Builders<Message>.Update.Set(m => m.IsRead, true);
            var result = await _messages.UpdateOneAsync(m => m.Id == messageId, update);
            
            if (result.ModifiedCount > 0)
                _logger.LogInformation("Message {MessageId} marked as read", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read: {MessageId}", messageId);
            throw;
        }
    }

    public async Task<int> GetUnreadMessageCountAsync(string userId)
    {
        try
        {
            var acceptedMatches = await GetAcceptedMatchesForUserAsync(userId);
            var matchIds = acceptedMatches.Select(m => m.Id!).ToList();
            
            if (!matchIds.Any())
                return 0;
            
            var filter = Builders<Message>.Filter.And(
                Builders<Message>.Filter.In(m => m.MatchId, matchIds),
                Builders<Message>.Filter.Eq(m => m.ReceiverId, userId),
                Builders<Message>.Filter.Eq(m => m.IsRead, false)
            );
            
            return (int)await _messages.CountDocumentsAsync(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread message count for user: {UserId}", userId);
            throw;
        }
    }

    // ==================== APPLICATION OPERATIONS ====================

    public async Task CreateApplicationAsync(IdeaApplication application)
    {
        try
        {
            application.AppliedAt = DateTime.UtcNow;
            await _applications.InsertOneAsync(application);
            
            // Increment applications count on the idea
            await IncrementIdeaApplicationsAsync(application.IdeaId);
            
            _logger.LogInformation("Application created: {ApplicationId} for idea: {IdeaId} by user: {UserId}", 
                application.Id, application.IdeaId, application.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application");
            throw;
        }
    }

    public async Task<List<IdeaApplication>> GetApplicationsForIdeaAsync(string ideaId)
    {
        try
        {
            if (!ObjectId.TryParse(ideaId, out _)) 
                return new List<IdeaApplication>();
                
            return await _applications.Find(a => a.IdeaId == ideaId)
                .SortByDescending(a => a.AppliedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applications for idea: {IdeaId}", ideaId);
            throw;
        }
    }

    /// <summary>
    /// Gets all applications submitted by a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of applications sorted by most recent first</returns>
    public async Task<List<IdeaApplication>> GetApplicationsByUserAsync(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out _)) 
                return new List<IdeaApplication>();
                
            var applications = await _applications.Find(a => a.UserId == userId)
                .SortByDescending(a => a.AppliedAt)
                .ToListAsync();
                
            _logger.LogDebug("Retrieved {Count} applications for user: {UserId}", applications.Count, userId);
            return applications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applications for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<IdeaApplication?> GetApplicationByIdAsync(string applicationId)
    {
        try
        {
            if (!ObjectId.TryParse(applicationId, out _)) 
                return null;
                
            return await _applications.Find(a => a.Id == applicationId).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application by ID: {ApplicationId}", applicationId);
            throw;
        }
    }

    public async Task UpdateApplicationStatusAsync(string applicationId, string status)
    {
        try
        {
            var update = Builders<IdeaApplication>.Update
                .Set(a => a.Status, status);
                // UpdatedAt removed as it's not in the model
                
            var result = await _applications.UpdateOneAsync(a => a.Id == applicationId, update);
            
            if (result.ModifiedCount > 0)
                _logger.LogInformation("Application {ApplicationId} status updated to: {Status}", applicationId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application status: {ApplicationId}", applicationId);
            throw;
        }
    }

    public async Task<bool> HasUserAppliedToIdeaAsync(string userId, string ideaId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out _) || !ObjectId.TryParse(ideaId, out _))
                return false;
                
            var count = await _applications.CountDocumentsAsync(a => a.UserId == userId && a.IdeaId == ideaId);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} applied to idea {IdeaId}", userId, ideaId);
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetApplicationCountsByStatusAsync(string ideaId)
    {
        try
        {
            if (!ObjectId.TryParse(ideaId, out _))
                return new Dictionary<string, int>();
                
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("IdeaId", ideaId)),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$Status" },
                    { "count", new BsonDocument("$sum", 1) }
                })
            };
            
            var results = await _applications.Aggregate<BsonDocument>(pipeline).ToListAsync();
            
            var counts = new Dictionary<string, int>();
            foreach (var result in results)
            {
                var status = result["_id"].AsString;
                var count = result["count"].AsInt32;
                counts[status] = count;
            }
            
            return counts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application counts for idea: {IdeaId}", ideaId);
            throw;
        }
    }

    // ==================== STATISTICS ====================

    public async Task<Dictionary<string, long>> GetStatisticsAsync()
    {
        try
        {
            var stats = new Dictionary<string, long>
            {
                ["TotalUsers"] = await _users.CountDocumentsAsync(_ => true),
                ["TotalIdeas"] = await _ideas.CountDocumentsAsync(_ => true),
                ["TotalMatches"] = await _matches.CountDocumentsAsync(_ => true),
                ["TotalMessages"] = await _messages.CountDocumentsAsync(_ => true),
                ["TotalApplications"] = await _applications.CountDocumentsAsync(_ => true),
                ["AcceptedMatches"] = await _matches.CountDocumentsAsync(m => m.Status == "accepted"),
                ["PendingMatches"] = await _matches.CountDocumentsAsync(m => m.Status == "pending"),
                ["RejectedMatches"] = await _matches.CountDocumentsAsync(m => m.Status == "rejected"),
                ["PendingApplications"] = await _applications.CountDocumentsAsync(a => a.Status == "pending"),
                ["AcceptedApplications"] = await _applications.CountDocumentsAsync(a => a.Status == "accepted"),
                ["RejectedApplications"] = await _applications.CountDocumentsAsync(a => a.Status == "rejected")
            };
            
            _logger.LogInformation("Statistics retrieved");
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            throw;
        }
    }
}