using System.Net.Http.Json;
using ByteDefence.Shared.DTOs;
using ByteDefence.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ByteDefence.Api.Services;

/// <summary>
/// Local notification service for development.
/// Uses HTTP POST to a self-hosted SignalR hub with retry logic.
/// </summary>
public class LocalNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalNotificationService> _logger;
    private readonly string _hubUrl;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalNotificationService(IConfiguration config, ILogger<LocalNotificationService> logger)
    {
        _logger = logger;
        _hubUrl = config["SignalR:HubUrl"] ?? "http://localhost:5000";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_hubUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {RetryCount} for SignalR broadcast after {Delay}s",
                        retryCount,
                        timeSpan.TotalSeconds);
                });

        _jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task BroadcastOrderUpdated(Order order)
    {
        await BroadcastAsync("OrderUpdated", $"order-{order.Id}", order.ToNotificationDto());
    }

    public async Task BroadcastOrderCreated(Order order)
    {
        await BroadcastAsync("OrderCreated", null, order.ToNotificationDto());
    }

    public async Task BroadcastOrderDeleted(string orderId)
    {
        await BroadcastAsync("OrderDeleted", null, new { OrderId = orderId });
    }

    private async Task BroadcastAsync(string method, string? group, object data)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var message = new SignalRMessage(method, group, data);
                var response = await _httpClient.PostAsJsonAsync("/api/broadcast", message, _jsonOptions);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Broadcasted {Method} to {Group}", method, group ?? "all");
            });
        }
        catch (Exception ex)
        {
            // Log but don't throw - notifications are best-effort
            _logger.LogWarning(ex,
                "Failed to broadcast {Method} after retries. SignalR hub may not be running at {HubUrl}.",
                method, _hubUrl);
        }
    }
}
