using ByteDefence.Shared.Models;

namespace ByteDefence.Api.Services;

public interface IOrderService
{
    Task<IQueryable<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(string id);
    Task<Order> CreateAsync(string title, string description, string userId);
    Task<Order?> UpdateAsync(string id, string? title, string? description, OrderStatus? status);
    Task<bool> DeleteAsync(string id);
    Task<OrderItem> AddItemAsync(string orderId, string name, int quantity, decimal price);
    Task<bool> RemoveItemAsync(string itemId);

    /// <summary>
    /// Gets the owner user ID for an order item (used for authorization checks).
    /// </summary>
    Task<string?> GetOrderOwnerByItemIdAsync(string itemId);
}

