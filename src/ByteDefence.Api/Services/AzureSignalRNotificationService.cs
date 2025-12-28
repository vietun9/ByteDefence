using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ByteDefence.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ByteDefence.Api.Services;

/// <summary>
/// Notification service for Azure SignalR Service.
/// Uses the Azure SignalR REST API to broadcast messages.
/// </summary>
public class AzureSignalRNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureSignalRNotificationService> _logger;
    private readonly string _hubName;
    private readonly string _connectionString;
    private readonly AsyncRetryPolicy _retryPolicy;

    public AzureSignalRNotificationService(
        IConfiguration config,
        ILogger<AzureSignalRNotificationService> logger)
    {
        _logger = logger;
        _connectionString = config["SignalR:ConnectionString"] 
            ?? throw new InvalidOperationException("SignalR:ConnectionString is required for Azure SignalR mode");
        _hubName = config["SignalR:HubName"] ?? "notifications";
        
        _httpClient = new HttpClient();

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
                        "Retry {RetryCount} for Azure SignalR broadcast after {Delay}s",
                        retryCount,
                        timeSpan.TotalSeconds);
                });
    }

    public async Task BroadcastOrderUpdated(Order order)
    {
        await BroadcastToGroupAsync($"order-{order.Id}", "OrderUpdated", order);
        await BroadcastToAllAsync("OrderUpdated", order);
    }

    public async Task BroadcastOrderCreated(Order order)
    {
        await BroadcastToAllAsync("OrderCreated", order);
    }

    public async Task BroadcastOrderDeleted(string orderId)
    {
        await BroadcastToAllAsync("OrderDeleted", new { OrderId = orderId });
    }

    private async Task BroadcastToAllAsync(string method, object data)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                var (endpoint, accessToken) = ParseConnectionString();
                var url = $"{endpoint}/api/v1/hubs/{_hubName}";

                var payload = new
                {
                    target = method,
                    arguments = new[] { data }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Broadcasted {Method} to all clients via Azure SignalR", method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast {Method} via Azure SignalR", method);
                throw;
            }
        });
    }

    private async Task BroadcastToGroupAsync(string group, string method, object data)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                var (endpoint, accessToken) = ParseConnectionString();
                var url = $"{endpoint}/api/v1/hubs/{_hubName}/groups/{group}";

                var payload = new
                {
                    target = method,
                    arguments = new[] { data }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Broadcasted {Method} to group {Group} via Azure SignalR", method, group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast {Method} to group {Group} via Azure SignalR", method, group);
                throw;
            }
        });
    }

    private (string endpoint, string accessKey) ParseConnectionString()
    {
        // Parse Azure SignalR connection string format:
        // Endpoint=https://xxx.service.signalr.net;AccessKey=xxx;Version=1.0;
        var parts = _connectionString.Split(';')
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1], StringComparer.OrdinalIgnoreCase);

        if (!parts.TryGetValue("Endpoint", out var endpoint))
            throw new InvalidOperationException("SignalR connection string missing Endpoint");
        
        if (!parts.TryGetValue("AccessKey", out var accessKey))
            throw new InvalidOperationException("SignalR connection string missing AccessKey");

        // Generate a simple access token (in production, use proper JWT generation)
        var token = GenerateAccessToken(endpoint, accessKey);
        
        return (endpoint, token);
    }

    private string GenerateAccessToken(string endpoint, string accessKey)
    {
        // For Azure SignalR REST API, we need to generate a JWT token
        // This is a simplified version - in production, use proper JWT libraries
        var audience = $"{endpoint}/api/v1/hubs/{_hubName}";
        var expiry = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        
        var claims = new Dictionary<string, object>
        {
            ["aud"] = audience,
            ["exp"] = expiry,
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // Use the access key to sign (simplified - use proper HMAC in production)
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(claims)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        
        using var hmac = new System.Security.Cryptography.HMACSHA256(Convert.FromBase64String(accessKey));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{header}.{payload}")))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return $"{header}.{payload}.{signature}";
    }
}
