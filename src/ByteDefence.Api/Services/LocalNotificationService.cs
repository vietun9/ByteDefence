using System.Net.Http.Json;
using ByteDefence.Shared.DTOs;
using ByteDefence.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ByteDefence.Api.Services;

public class LocalNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalNotificationService> _logger;
    private readonly string _hubUrl;

    public LocalNotificationService(IConfiguration config, ILogger<LocalNotificationService> logger)
    {
        _logger = logger;
        _hubUrl = config["SignalR:HubUrl"] ?? "http://localhost:5000";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_hubUrl)
        };
    }

    public async Task BroadcastOrderUpdated(Order order)
    {
        await BroadcastAsync("OrderUpdated", $"order-{order.Id}", order);
    }

    public async Task BroadcastOrderCreated(Order order)
    {
        await BroadcastAsync("OrderCreated", null, order);
    }

    public async Task BroadcastOrderDeleted(string orderId)
    {
        await BroadcastAsync("OrderDeleted", null, new { OrderId = orderId });
    }

    private async Task BroadcastAsync(string method, string? group, object data)
    {
        try
        {
            var message = new SignalRMessage(method, group, data);
            await _httpClient.PostAsJsonAsync("/api/broadcast", message);
            _logger.LogInformation("Broadcasted {Method} to {Group}", method, group ?? "all");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast {Method}. SignalR hub may not be running.", method);
        }
    }
}
