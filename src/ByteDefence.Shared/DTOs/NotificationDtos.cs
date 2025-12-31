using ByteDefence.Shared.Models;

namespace ByteDefence.Shared.DTOs;

public record UserDto(string Id, string Username, string Email, UserRole Role);

public record OrderItemDto(string Id, string Name, int Quantity, decimal Price, decimal Subtotal);

public record OrderNotificationDto(
    string Id,
    string Title,
    string Description,
    OrderStatus Status,
    List<OrderItemDto> Items,
    decimal Total,
    UserDto? CreatedBy,
    DateTime UpdatedAt);

public static class NotificationMappingExtensions
{
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Username, user.Email, user.Role);

    public static OrderItemDto ToDto(this OrderItem item) =>
        new(item.Id, item.Name, item.Quantity, item.Price, item.Price * item.Quantity);

    public static OrderNotificationDto ToNotificationDto(this Order order) =>
        new(
            order.Id,
            order.Title,
            order.Description,
            order.Status,
            [.. order.Items.Select(i => i.ToDto())],
            order.Total,
            order.CreatedBy?.ToDto(),
            order.UpdatedAt
        );
}
