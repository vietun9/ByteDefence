using Microsoft.AspNetCore.SignalR;

namespace ByteDefence.SignalR.Hubs;

public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinOrderGroup(string orderId)
    {
        var groupName = $"order-{orderId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveOrderGroup(string orderId)
    {
        var groupName = $"order-{orderId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task JoinAllOrdersGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-orders");
        _logger.LogInformation("Client {ConnectionId} joined all-orders group", Context.ConnectionId);
    }

    public async Task LeaveAllOrdersGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all-orders");
        _logger.LogInformation("Client {ConnectionId} left all-orders group", Context.ConnectionId);
    }
}
