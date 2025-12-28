using ByteDefence.Api.Data;
using ByteDefence.Api.Services;
using ByteDefence.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ByteDefence.Api.GraphQL.Schema.Queries;

public class OrderQueryResolver
{
    /// <summary>
    /// Get all orders with optional filtering and pagination.
    /// Demonstrates concurrent query pattern with EF Core.
    /// </summary>
    [UseFiltering]
    [UseSorting]
    public async Task<IEnumerable<Order>> GetOrders(
        [Service] IOrderService orderService,
        [GlobalState("CurrentUser")] string? userId)
    {
        // Check if user is authenticated
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Authentication required");
        }

        var ordersQuery = await orderService.GetAllAsync();
        return await ordersQuery.ToListAsync();
    }

    /// <summary>
    /// Get a single order by ID.
    /// </summary>
    public async Task<Order?> GetOrder(
        string id,
        [Service] IOrderService orderService,
        [GlobalState("CurrentUser")] string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Authentication required");
        }

        return await orderService.GetByIdAsync(id);
    }

    /// <summary>
    /// Get the currently authenticated user.
    /// Requires authentication.
    /// </summary>
    public async Task<User?> GetMe(
        [Service] IUserService userService,
        [GlobalState("CurrentUser")] string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;
        return await userService.GetByIdAsync(userId);
    }

    /// <summary>
    /// Demonstrates concurrent database queries.
    /// Returns orders and users count in parallel.
    /// </summary>
    public async Task<OrderStats> GetOrderStats(
        [Service] AppDbContext context,
        [GlobalState("CurrentUser")] string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Authentication required");
        }

        // Execute concurrent queries
        var ordersCountTask = context.Orders.CountAsync();
        var usersCountTask = context.Users.CountAsync();
        var pendingOrdersTask = context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        var totalValueTask = context.OrderItems.SumAsync(i => i.Price * i.Quantity);

        await Task.WhenAll(ordersCountTask, usersCountTask, pendingOrdersTask, totalValueTask);

        return new OrderStats
        {
            TotalOrders = await ordersCountTask,
            TotalUsers = await usersCountTask,
            PendingOrders = await pendingOrdersTask,
            TotalValue = await totalValueTask
        };
    }
}

public class OrderStats
{
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public int PendingOrders { get; set; }
    public decimal TotalValue { get; set; }
}
