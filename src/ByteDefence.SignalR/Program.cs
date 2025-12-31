using ByteDefence.SignalR.Hubs;
using ByteDefence.Shared.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Get environment - require JWT secret in production
var environment = builder.Environment.EnvironmentName;
var jwtSecret = builder.Configuration["Jwt:Secret"];

if (environment != "Development" && string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException(
        "JWT secret must be configured via Jwt:Secret in production. " +
        "Do not rely on default development secrets.");
}

// Use development secret only in Development environment
jwtSecret ??= "ByteDefence-Super-Secret-Key-For-Development-Only-32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ByteDefence";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ByteDefence-API";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Configure JWT Bearer for SignalR (token from query string)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS - Note: AllowAnyOrigin is used for local development only.
// In production, this should be restricted to specific domains via configuration.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:5001", "http://localhost:7071" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Map the SignalR hub
// NOTE: Anonymous connections allowed for development/demo purposes.
// In production, apply [Authorize] attribute to NotificationHub class
// or use .RequireAuthorization() below to require authentication.
// For this demo, we allow anonymous so users can see real-time updates
// without requiring login for the SignalR connection itself.
app.MapHub<NotificationHub>("/hubs/notifications");

// API endpoint for broadcasting from the Azure Functions
// This should be secured in production (e.g., with a shared secret header)
app.MapPost("/api/broadcast", async (SignalRMessage message, IHubContext<NotificationHub> hubContext, HttpContext httpContext) =>
{
    // Optional: Validate internal API key for production
    var apiKey = httpContext.Request.Headers["X-Internal-Api-Key"].FirstOrDefault();
    var expectedKey = builder.Configuration["SignalR:InternalApiKey"];
    if (!string.IsNullOrEmpty(expectedKey) && apiKey != expectedKey)
    {
        return Results.Unauthorized();
    }

    if (!string.IsNullOrEmpty(message.Group))
    {
        await hubContext.Clients.Group(message.Group).SendAsync(message.Method, message.Data);
    }
    else
    {
        // Broadcast to all-orders group for list updates
        await hubContext.Clients.Group("all-orders").SendAsync(message.Method, message.Data);
        // Also broadcast to all clients
        await hubContext.Clients.All.SendAsync(message.Method, message.Data);
    }

    return Results.Ok();
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "signalr-hub" }));

app.Run();
