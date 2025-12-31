namespace ByteDefence.Shared.Models;

public class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public List<OrderItem> Items { get; set; } = new();
    public string CreatedById { get; set; } = string.Empty;
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public decimal Total => Items.Sum(i => i.Price * i.Quantity);
}
