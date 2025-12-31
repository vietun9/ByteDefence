using ByteDefence.Api.Data;
using ByteDefence.Api.Services;
using ByteDefence.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ByteDefence.Api.GraphQL.Schema.Queries;

public class OrderQueryResolver
{
    /// <summary>
    /// Get all orders with optional filtering and pagination.
    /// Admin sees all orders; regular users see only their own orders.
    /// </summary>
    [UseFiltering]
    [UseSorting]
    public async Task<IEnumerable<Order>> GetOrders(
        [Service] IOrderService orderService,
        [GlobalState("CurrentUser")] string? userId,
        [GlobalState("CurrentRole")] string? role)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Authentication required");
        }

        var query = await orderService.GetAllAsync();

        // Non-admin users can only see their own orders
        if (role != "Admin")
        {
            query = query.Where(o => o.CreatedById == userId);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Get a single order by ID.
    /// Only the order owner or an admin can view order details.
    /// </summary>
    public async Task<Order?> GetOrder(
        string id,
        [Service] IOrderService orderService,
        [GlobalState("CurrentUser")] string? userId,
        [GlobalState("CurrentRole")] string? role)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Authentication required");
        }

        var order = await orderService.GetByIdAsync(id);
        if (order == null) return null;

        // Check authorization: must be owner or admin
        if (role != "Admin" && order.CreatedById != userId)
        {
            throw new UnauthorizedAccessException("You can only view your own orders");
        }

        return order;
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
    /// Returns orders and users count in parallel using Task.WhenAll.
    /// </summary>
    public async Task<OrderStats> GetOrderStats(
        [Service] IDbContextFactory<AppDbContext> contextFactory,
        [GlobalState("CurrentUser")] string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Authentication required");
        }

        // Execute all queries concurrently using separate DbContext instances
        var totalOrdersTask = Task.Run(async () =>
        {
            await using var ctx = await contextFactory.CreateDbContextAsync();
            return await ctx.Orders.CountAsync();
        });

        var totalUsersTask = Task.Run(async () =>
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

        // Wait for all queries to complete
        await Task.WhenAll(totalOrdersTask, totalUsersTask, pendingOrdersTask, totalValueTask);

        return new OrderStats
        {
            TotalOrders = await totalOrdersTask,
            TotalUsers = await totalUsersTask,
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
