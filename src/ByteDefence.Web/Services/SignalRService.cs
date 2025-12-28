using Microsoft.AspNetCore.SignalR.Client;

namespace ByteDefence.Web.Services;

public interface ISignalRService : IAsyncDisposable
{
    HubConnectionState State { get; }
    event Action<object>? OnOrderCreated;
    event Action<object>? OnOrderUpdated;
    event Action<string>? OnOrderDeleted;
    event Action<HubConnectionState>? OnStateChanged;
    Task StartAsync();
    Task StopAsync();
    Task JoinOrderGroupAsync(string orderId);
    Task LeaveOrderGroupAsync(string orderId);
    Task JoinAllOrdersGroupAsync();
    Task LeaveAllOrdersGroupAsync();
}

public class SignalRService : ISignalRService
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<SignalRService> _logger;

    public HubConnectionState State => _hubConnection.State;

    public event Action<object>? OnOrderCreated;
    public event Action<object>? OnOrderUpdated;
    public event Action<string>? OnOrderDeleted;
    public event Action<HubConnectionState>? OnStateChanged;

    public SignalRService(IConfiguration configuration, ILogger<SignalRService> logger)
    {
        _logger = logger;
        var hubUrl = configuration["SignalR:HubUrl"] ?? "http://localhost:5000/hubs/notifications";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        _hubConnection.On<object>("OrderCreated", data =>
        {
            _logger.LogInformation("Received OrderCreated event");
            OnOrderCreated?.Invoke(data);
        });

        _hubConnection.On<object>("OrderUpdated", data =>
        {
            _logger.LogInformation("Received OrderUpdated event");
            OnOrderUpdated?.Invoke(data);
        });

        _hubConnection.On<object>("OrderDeleted", data =>
        {
            _logger.LogInformation("Received OrderDeleted event");
            if (data is System.Text.Json.JsonElement jsonElement && 
                jsonElement.TryGetProperty("orderId", out var orderIdElement))
            {
                OnOrderDeleted?.Invoke(orderIdElement.GetString() ?? string.Empty);
            }
        });

        _hubConnection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "SignalR reconnecting...");
            OnStateChanged?.Invoke(HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("SignalR reconnected: {ConnectionId}", connectionId);
            OnStateChanged?.Invoke(HubConnectionState.Connected);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += error =>
        {
            _logger.LogWarning(error, "SignalR connection closed");
            OnStateChanged?.Invoke(HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync()
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("SignalR connected");
                OnStateChanged?.Invoke(HubConnectionState.Connected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to SignalR hub");
                OnStateChanged?.Invoke(HubConnectionState.Disconnected);
            }
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
            OnStateChanged?.Invoke(HubConnectionState.Disconnected);
        }
    }

    public async Task JoinOrderGroupAsync(string orderId)
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinOrderGroup", orderId);
        }
    }

    public async Task LeaveOrderGroupAsync(string orderId)
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveOrderGroup", orderId);
        }
    }

    public async Task JoinAllOrdersGroupAsync()
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinAllOrdersGroup");
        }
    }

    public async Task LeaveAllOrdersGroupAsync()
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveAllOrdersGroup");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
