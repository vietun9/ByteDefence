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
    /// Demonstrates concurrent database queries using separate DbContext instances.
    /// EF Core DbContext is not thread-safe, so we use IDbContextFactory for parallel queries.
    /// Returns orders and users count in parallel.
    /// </summary>
    public async Task<OrderStats> GetOrderStats(
        [Service] IDbContextFactory<AppDbContext> contextFactory,
        [GlobalState("CurrentUser")] string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Authentication required");
        }

        // Execute concurrent queries with separate DbContext instances (thread-safe)
        var ordersCountTask = Task.Run(async () =>
        {
            await using var ctx = await contextFactory.CreateDbContextAsync();
            return await ctx.Orders.CountAsync();
        });

        var usersCountTask = Task.Run(async () =>
        {
            await using var ctx = await contextFactory.CreateDbContextAsync();
            return await ctx.Users.CountAsync();
        });

        var pendingOrdersTask = Task.Run(async () =>
        {
            await using var ctx = await contextFactory.CreateDbContextAsync();
            return await ctx.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        });

        var totalValueTask = Task.Run(async () =>
        {
            await using var ctx = await contextFactory.CreateDbContextAsync();
            return await ctx.OrderItems.SumAsync(i => i.Price * i.Quantity);
        });

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
