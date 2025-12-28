using ByteDefence.Shared.Models;

namespace ByteDefence.Shared.DTOs;

public record CreateOrderInput(string Title, string Description);

public record CreateOrderPayload(Order? Order, string? ErrorMessage = null);

public record UpdateOrderInput(string Id, string? Title, string? Description, OrderStatus? Status);

public record UpdateOrderPayload(Order? Order, string? ErrorMessage = null);

public record DeleteOrderPayload(bool Success, string? ErrorMessage = null);

public record AddOrderItemInput(string OrderId, string Name, int Quantity, decimal Price);

public record AddOrderItemPayload(OrderItem? Item, string? ErrorMessage = null);

public record RemoveOrderItemPayload(bool Success, string? ErrorMessage = null);

public record LoginInput(string Username, string Password);

public record LoginPayload(string? Token, User? User, string? ErrorMessage = null);
