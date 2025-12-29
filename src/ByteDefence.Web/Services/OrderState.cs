using System.Collections.Concurrent;

namespace ByteDefence.Web.Services;

public class OrderState
{
    private readonly ConcurrentDictionary<string, OrderDto> _orders = new();

    public event Action? OnChange;

    public void Upsert(OrderDto order)
    {
        if (order == null) return;
        _orders.AddOrUpdate(order.Id, order, (_, __) => order);
        OnChange?.Invoke();
    }

    public void Remove(string orderId)
    {
        _orders.TryRemove(orderId, out _);
        OnChange?.Invoke();
    }

    public OrderDto? Get(string orderId)
    {
        _orders.TryGetValue(orderId, out var order);
        return order;
    }

    public IReadOnlyCollection<OrderDto> GetAll() => _orders.Values.ToList();
}
