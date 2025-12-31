using ByteDefence.Shared.Models;

namespace ByteDefence.Api.Services;

public interface INotificationService
{
    Task BroadcastOrderUpdated(Order order);
    Task BroadcastOrderCreated(Order order);
    Task BroadcastOrderDeleted(string orderId);
}
