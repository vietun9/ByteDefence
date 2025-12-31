using ByteDefence.Api.Data;
using ByteDefence.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ByteDefence.Api.Services;

public class OrderService(AppDbContext context, INotificationService notificationService) : IOrderService
{
    private readonly AppDbContext _context = context;
    private readonly INotificationService _notificationService = notificationService;

    public Task<IQueryable<Order>> GetAllAsync()
    {
        return Task.FromResult(_context.Orders
            .Include(o => o.Items)
            .Include(o => o.CreatedBy)
            .AsQueryable());
    }

    public async Task<Order?> GetByIdAsync(string id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.CreatedBy)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order> CreateAsync(string title, string description, string userId)
    {
        var order = new Order
        {
            Title = title,
            Description = description,
            CreatedById = userId,
            Status = OrderStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        order = await GetByIdAsync(order.Id) ?? order;

        await _notificationService.BroadcastOrderCreated(order);

        return order;
    }

    public async Task<Order?> UpdateAsync(string id, string? title, string? description, OrderStatus? status)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.CreatedBy)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return null;

        if (title != null) order.Title = title;
        if (description != null) order.Description = description;
        if (status != null) order.Status = status.Value;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _notificationService.BroadcastOrderUpdated(order);

        return order;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return false;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        await _notificationService.BroadcastOrderDeleted(id);

        return true;
    }

    public async Task<OrderItem> AddItemAsync(string orderId, string name, int quantity, decimal price)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId) ?? throw new InvalidOperationException($"Order {orderId} not found");
        var item = new OrderItem
        {
            OrderId = orderId,
            Name = name,
            Quantity = quantity,
            Price = price
        };

        _context.OrderItems.Add(item);
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Reload order for broadcast
        order = await GetByIdAsync(orderId) ?? order;
        await _notificationService.BroadcastOrderUpdated(order);

        return item;
    }

    public async Task<bool> RemoveItemAsync(string itemId)
    {
        var item = await _context.OrderItems.FindAsync(itemId);
        if (item == null) return false;

        var orderId = item.OrderId;
        _context.OrderItems.Remove(item);
        await _context.SaveChangesAsync();

        var order = await GetByIdAsync(orderId);
        if (order != null)
        {
            await _notificationService.BroadcastOrderUpdated(order);
        }

        return true;
    }

    /// <summary>
    /// Gets the owner user ID for an order item by looking up the parent order.
    /// </summary>
    public async Task<string?> GetOrderOwnerByItemIdAsync(string itemId)
    {
        var item = await _context.OrderItems
            .Include(i => i.Order)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        return item?.Order?.CreatedById;
    }
}

