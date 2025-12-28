using ByteDefence.SignalR.Hubs;
using ByteDefence.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
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
