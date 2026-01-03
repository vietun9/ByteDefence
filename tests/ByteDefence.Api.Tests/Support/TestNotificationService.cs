using ByteDefence.Api.Services;
using ByteDefence.Shared.Models;
using System.Threading.Tasks;

namespace ByteDefence.Api.Tests.Support;

/// <summary>
/// Test double for notifications: no-op to avoid network calls/timeouts during integration tests.
/// </summary>
public class TestNotificationService : INotificationService
{
    public Task BroadcastOrderCreated(Order order) => Task.CompletedTask;
    public Task BroadcastOrderDeleted(string orderId) => Task.CompletedTask;
    public Task BroadcastOrderUpdated(Order order) => Task.CompletedTask;
}
