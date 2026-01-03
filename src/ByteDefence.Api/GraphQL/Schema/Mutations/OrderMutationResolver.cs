using ByteDefence.Api.Services;
using ByteDefence.Shared.DTOs;

namespace ByteDefence.Api.GraphQL.Schema.Mutations;

[ExtendObjectType("Mutation")]
public class OrderMutationResolver
{
    /// <summary>
    /// Create a new order.
    /// </summary>
    public async Task<CreateOrderPayload> CreateOrder(
        CreateOrderInput input,
        [Service] IOrderService orderService,
        [GlobalState("CurrentUser")] string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new CreateOrderPayload(null, "User not authenticated");
        }

        if (string.IsNullOrWhiteSpace(input.Title))
        {
            return new CreateOrderPayload(null, "Title is required");
        }

        try
        {
            var order = await orderService.CreateAsync(input.Title, input.Description, userId);
            return new CreateOrderPayload(order);
        }
        catch (Exception ex)
        {
            return new CreateOrderPayload(null, ex.Message);
        }
    }

    /// <summary>
    /// Update an existing order.
    /// Only the order owner or an admin can update the order.
    /// </summary>
    public async Task<UpdateOrderPayload> UpdateOrder(
        UpdateOrderInput input,
        [Service] IOrderService orderService,
        [GlobalState("CurrentUser")] string? userId,
        [GlobalState("CurrentRole")] string? role)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new UpdateOrderPayload(null, "User not authenticated");
        }

        if (string.IsNullOrWhiteSpace(input.Id))
        {
            return new UpdateOrderPayload(null, "Order ID is required");
        }

        try
        {
            // Check ownership before updating
            var existingOrder = await orderService.GetByIdAsync(input.Id);
            if (existingOrder == null)
            {
                return new UpdateOrderPayload(null, $"Order {input.Id} not found");
            }

            // Authorization check: must be owner or admin
            if (role != "Admin" && existingOrder.CreatedById != userId)
            {
                return new UpdateOrderPayload(null, "You can only update your own orders");
            }

            var order = await orderService.UpdateAsync(input.Id, input.Title, input.Description, input.Status);
            return new UpdateOrderPayload(order);
        }
        catch (Exception ex)
        {
            return new UpdateOrderPayload(null, ex.Message);
        }
    }

    /// <summary>
    /// Delete an order. Requires Admin role.
    /// </summary>
    public async Task<DeleteOrderPayload> DeleteOrder(
        string id,
        [Service] IOrderService orderService,
        [GlobalState("CurrentUser")] string? userId,
        [GlobalState("CurrentRole")] string? role)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new DeleteOrderPayload(false, "User not authenticated");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return new DeleteOrderPayload(false, "Order ID is required");
        }

        // Admin-only check
        if (role != "Admin")
        {
            return new DeleteOrderPayload(false, "Only administrators can delete orders");
        }

        try
        {
            var success = await orderService.DeleteAsync(id);
            if (!success)
            {
                return new DeleteOrderPayload(false, $"Order {id} not found");
            }
            return new DeleteOrderPayload(true);
        }
        catch (Exception ex)
        {
            return new DeleteOrderPayload(false, ex.Message);
        }
    }

    /// <summary>
    /// Add an item to an existing order.
    /// Only the order owner or an admin can add items.
    /// </summary>
    public async Task<AddOrderItemPayload> AddOrderItem(
        AddOrderItemInput input,
        [Service] IOrderService orderService,
        [GlobalState("CurrentUser")] string? userId,
        [GlobalState("CurrentRole")] string? role)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new AddOrderItemPayload(null, "User not authenticated");
        }

        if (string.IsNullOrWhiteSpace(input.OrderId))
        {
            return new AddOrderItemPayload(null, "Order ID is required");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return new AddOrderItemPayload(null, "Item name is required");
        }

        if (input.Quantity <= 0)
        {
            return new AddOrderItemPayload(null, "Quantity must be greater than zero");
        }

        if (input.Price < 0)
        {
            return new AddOrderItemPayload(null, "Price cannot be negative");
        }

        try
        {
            // Check ownership before adding item
            var order = await orderService.GetByIdAsync(input.OrderId);
            if (order == null)
            {
                return new AddOrderItemPayload(null, $"Order {input.OrderId} not found");
            }

            // Authorization check: must be owner or admin
            if (role != "Admin" && order.CreatedById != userId)
            {
                return new AddOrderItemPayload(null, "You can only add items to your own orders");
            }

            var item = await orderService.AddItemAsync(input.OrderId, input.Name, input.Quantity, input.Price);
            return new AddOrderItemPayload(item);
        }
        catch (Exception ex)
        {
            return new AddOrderItemPayload(null, ex.Message);
        }
    }

    /// <summary>
    /// Remove an item from an order.
    /// Only the order owner or an admin can remove items.
    /// </summary>
    public async Task<RemoveOrderItemPayload> RemoveOrderItem(
        string itemId,
        [Service] IOrderService orderService,
        [GlobalState("CurrentUser")] string? userId,
        [GlobalState("CurrentRole")] string? role)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new RemoveOrderItemPayload(false, "User not authenticated");
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return new RemoveOrderItemPayload(false, "Item ID is required");
        }

        try
        {
            // Check ownership before removing item
            var ownerId = await orderService.GetOrderOwnerByItemIdAsync(itemId);
            if (ownerId == null)
            {
                return new RemoveOrderItemPayload(false, $"Item {itemId} not found");
            }

            // Authorization check: must be owner or admin
            if (role != "Admin" && ownerId != userId)
            {
                return new RemoveOrderItemPayload(false, "You can only remove items from your own orders");
            }

            var success = await orderService.RemoveItemAsync(itemId);
            if (!success)
            {
                return new RemoveOrderItemPayload(false, $"Item {itemId} not found");
            }
            return new RemoveOrderItemPayload(true);
        }
        catch (Exception ex)
        {
            return new RemoveOrderItemPayload(false, ex.Message);
        }
    }
}

