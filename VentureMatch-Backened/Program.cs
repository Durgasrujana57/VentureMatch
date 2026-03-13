using CoFounderFinder.Api.Helpers;
using CoFounderFinder.Api.Models;
using CoFounderFinder.Api.Services;
using CoFounderFinder.Api.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.SwaggerGen; // Add this for IDocumentFilter
using Microsoft.OpenApi.Any; // Add this for OpenApiString

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add SignalR for real-time messaging
builder.Services.AddSignalR();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CoFounder Finder API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter 'Bearer' [space] and your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new List<string>()
        }
    });
    
    // Add SignalR support info to Swagger
    c.DocumentFilter<SignalRSwaggerFilter>();
});

// MongoDB
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<JwtHelper>();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJWTTokenThatIsAtLeast32CharsLong!";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    
    // Important for SignalR - allows reading token from query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            
            // If the request is for our SignalR hub
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            
            return Task.CompletedTask;
        }
    };
});

// CORS - Updated for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>("mongodb");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoFounder Finder API V1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<ChatHub>("/hubs/chat");

// Map health checks
app.MapHealthChecks("/health");

// Simple test endpoint
app.MapGet("/", () => "CoFounder Finder API is running!");

app.Run();

// Health check implementation
public class MongoDbHealthCheck : IHealthCheck
{
    private readonly MongoDbService _mongoDbService;
    private readonly ILogger<MongoDbHealthCheck> _logger;

    public MongoDbHealthCheck(MongoDbService mongoDbService, ILogger<MongoDbHealthCheck> logger)
    {
        _mongoDbService = mongoDbService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get database statistics as a health check
            var stats = await _mongoDbService.GetStatisticsAsync();
            
            var data = new Dictionary<string, object>
            {
                { "totalUsers", stats["TotalUsers"] },
                { "totalMatches", stats["TotalMatches"] },
                { "totalMessages", stats["TotalMessages"] },
                { "database", "connected" }
            };
            
            return HealthCheckResult.Healthy("MongoDB is connected", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB health check failed");
            return HealthCheckResult.Unhealthy("MongoDB connection failed", ex);
        }
    }
}

// Optional: Swagger filter to document SignalR hub
public class SignalRSwaggerFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var hubPath = "/hubs/chat";
        
        // Add WebSocket information to the document
        if (!swaggerDoc.Paths.ContainsKey(hubPath))
        {
            var pathItem = new OpenApiPathItem();
            
            var operation = new OpenApiOperation
            {
                Tags = new List<OpenApiTag> { new OpenApiTag { Name = "SignalR" } },
                Summary = "SignalR Chat Hub",
                Description = "WebSocket connection for real-time chat. Connect using: ws://localhost:5212/hubs/chat?access_token=YOUR_JWT_TOKEN",
                Responses = new OpenApiResponses
                {
                    ["101"] = new OpenApiResponse
                    {
                        Description = "Switching Protocols - WebSocket connection established",
                        Content = new Dictionary<string, OpenApiMediaType>()
                    }
                }
            };
            
            // Add a parameter for the access token
            operation.Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter
                {
                    Name = "access_token",
                    In = ParameterLocation.Query,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "JWT token for authentication"
                }
            };
            
            pathItem.AddOperation(OperationType.Get, operation);
            swaggerDoc.Paths.Add(hubPath, pathItem);
        }
    }
}