using ByteDefence.Shared.Models;

namespace ByteDefence.Web.Services;

public interface IOrderService
{
    Task<OrdersResponse?> GetOrdersAsync();
    Task<OrderResponse?> GetOrderAsync(string id);
    Task<OrderStatsResponse?> GetOrderStatsAsync();
    Task<CreateOrderResponse?> CreateOrderAsync(string title, string description);
    Task<UpdateOrderResponse?> UpdateOrderAsync(string id, string? title, string? description, OrderStatus? status);
    Task<DeleteOrderResponse?> DeleteOrderAsync(string id);
    Task<AddItemResponse?> AddOrderItemAsync(string orderId, string name, int quantity, decimal price);
    Task<RemoveItemResponse?> RemoveOrderItemAsync(string itemId);
}

public class OrderService : IOrderService
{
    private readonly IGraphQLClient _graphQLClient;

    public OrderService(IGraphQLClient graphQLClient)
    {
        _graphQLClient = graphQLClient;
    }

    public async Task<OrdersResponse?> GetOrdersAsync()
    {
        const string query = @"
            query GetOrders {
                orders {
                    id
                    title
                    description
                    status
                    total
                    createdAt
                    updatedAt
                    items {
                        id
                        name
                        quantity
                        price
                        subtotal
                    }
                    createdBy {
                        id
                        username
                        email
                    }
                }
            }";

        var response = await _graphQLClient.QueryAsync<OrdersResponse>(query);
        return response.Data;
    }

    public async Task<OrderResponse?> GetOrderAsync(string id)
    {
        const string query = @"
            query GetOrder($id: String!) {
                order(id: $id) {
                    id
                    title
                    description
                    status
                    total
                    createdAt
                    updatedAt
                    items {
                        id
                        name
                        quantity
                        price
                        subtotal
                    }
                    createdBy {
                        id
                        username
                        email
                    }
                }
            }";

        var response = await _graphQLClient.QueryAsync<OrderResponse>(query, new { id });
        return response.Data;
    }

    public async Task<OrderStatsResponse?> GetOrderStatsAsync()
    {
        const string query = @"
            query GetOrderStats {
                orderStats {
                    totalOrders
                    totalUsers
                    pendingOrders
                    totalValue
                }
            }";

        var response = await _graphQLClient.QueryAsync<OrderStatsResponse>(query);
        return response.Data;
    }

    public async Task<CreateOrderResponse?> CreateOrderAsync(string title, string description)
    {
        const string mutation = @"
            mutation CreateOrder($input: CreateOrderInput!) {
                createOrder(input: $input) {
                    order {
                        id
                        title
                        description
                        status
                        total
                        createdAt
                        updatedAt
                        items {
                            id
                            name
                            quantity
                            price
                            subtotal
                        }
                        createdBy {
                            id
                            username
                            email
                        }
                    }
                    errorMessage
                }
            }";

        var response = await _graphQLClient.MutateAsync<CreateOrderResponse>(mutation,
            new { input = new { title, description } });
        return response.Data;
    }

    public async Task<UpdateOrderResponse?> UpdateOrderAsync(string id, string? title, string? description, OrderStatus? status)
    {
        const string mutation = @"
            mutation UpdateOrder($input: UpdateOrderInput!) {
                updateOrder(input: $input) {
                    order {
                        id
                        title
                        description
                        status
                        createdAt
                        updatedAt
                        items {
                            id
                            name
                            quantity
                            price
                            subtotal
                        }
                        createdBy {
                            id
                            username
                            email
                        }
                    }
                    errorMessage
                }
            }";

        var response = await _graphQLClient.MutateAsync<UpdateOrderResponse>(mutation,
            new { input = new { id, title, description, status = status?.ToString().ToUpper() } });
        return response.Data;
    }

    public async Task<DeleteOrderResponse?> DeleteOrderAsync(string id)
    {
        const string mutation = @"
            mutation DeleteOrder($id: String!) {
                deleteOrder(id: $id) {
                    success
                    errorMessage
                }
            }";

        var response = await _graphQLClient.MutateAsync<DeleteOrderResponse>(mutation, new { id });
        return response.Data;
    }

    public async Task<AddItemResponse?> AddOrderItemAsync(string orderId, string name, int quantity, decimal price)
    {
        const string mutation = @"
            mutation AddOrderItem($input: AddOrderItemInput!) {
                addOrderItem(input: $input) {
                    item {
                        id
                        name
                        quantity
                        price
                        subtotal
                    }
                    errorMessage
                }
            }";

        var response = await _graphQLClient.MutateAsync<AddItemResponse>(mutation,
            new { input = new { orderId, name, quantity, price } });
        return response.Data;
    }

    public async Task<RemoveItemResponse?> RemoveOrderItemAsync(string itemId)
    {
        const string mutation = @"
            mutation RemoveOrderItem($itemId: String!) {
                removeOrderItem(itemId: $itemId) {
                    success
                    errorMessage
                }
            }";

        var response = await _graphQLClient.MutateAsync<RemoveItemResponse>(mutation, new { itemId });
        return response.Data;
    }
}

// Response DTOs
public class OrdersResponse
{
    public List<OrderDto> Orders { get; set; } = new();
}

public class OrderResponse
{
    public OrderDto? Order { get; set; }
}

public class OrderStatsResponse
{
    public OrderStatsDto? OrderStats { get; set; }
}

public class CreateOrderResponse
{
    public CreateOrderPayloadDto? CreateOrder { get; set; }
}

public class UpdateOrderResponse
{
    public UpdateOrderPayloadDto? UpdateOrder { get; set; }
}

public class DeleteOrderResponse
{
    public DeleteOrderPayloadDto? DeleteOrder { get; set; }
}

public class AddItemResponse
{
    public AddItemPayloadDto? AddOrderItem { get; set; }
}

public class RemoveItemResponse
{
    public RemoveItemPayloadDto? RemoveOrderItem { get; set; }
}

public class OrderDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public UserDto? CreatedBy { get; set; }
}

public class OrderItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Subtotal { get; set; }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class OrderStatsDto
{
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public int PendingOrders { get; set; }
    public decimal TotalValue { get; set; }
}

public class CreateOrderPayloadDto
{
    public OrderDto? Order { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UpdateOrderPayloadDto
{
    public OrderDto? Order { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DeleteOrderPayloadDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AddItemPayloadDto
{
    public OrderItemDto? Item { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RemoveItemPayloadDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
