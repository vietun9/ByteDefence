using ByteDefence.SignalR.Hubs;
using ByteDefence.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

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

// Map the SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

// API endpoint for broadcasting from the Azure Functions
app.MapPost("/api/broadcast", async (SignalRMessage message, IHubContext<NotificationHub> hubContext) =>
{
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
