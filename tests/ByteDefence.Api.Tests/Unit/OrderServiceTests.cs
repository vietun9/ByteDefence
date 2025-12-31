using ByteDefence.Api.Data;
using ByteDefence.Api.Services;
using ByteDefence.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ByteDefence.Api.Tests.Unit;

public class OrderServiceTests
{
    private readonly AppDbContext _context;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _notificationServiceMock = new Mock<INotificationService>();
        _orderService = new OrderService(_context, _notificationServiceMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        var user = new User
        {
            Id = "user-test",
            Username = "testuser",
            Email = "test@test.com",
            Role = UserRole.User
        };
        _context.Users.Add(user);

        var order = new Order
        {
            Id = "order-test",
            Title = "Test Order",
            Description = "Test Description",
            Status = OrderStatus.Draft,
            CreatedById = user.Id
        };
        _context.Orders.Add(order);

        var item = new OrderItem
        {
            Id = "item-test",
            OrderId = order.Id,
            Name = "Test Item",
            Quantity = 2,
            Price = 10.00m
        };
        _context.OrderItems.Add(item);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrders()
    {
        // Act
        var result = await _orderService.GetAllAsync();
        var orders = await result.ToListAsync();

        // Assert
        orders.Should().NotBeEmpty();
        orders.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsOrder()
    {
        // Act
        var result = await _orderService.GetByIdAsync("order-test");

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Order");
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _orderService.GetByIdAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesNewOrder()
    {
        // Arrange
        var title = "New Order";
        var description = "New Description";
        var userId = "user-test";

        // Act
        var result = await _orderService.CreateAsync(title, description, userId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(title);
        result.Description.Should().Be(description);
        result.Status.Should().Be(OrderStatus.Draft);
        result.CreatedById.Should().Be(userId);

        _notificationServiceMock.Verify(
            x => x.BroadcastOrderCreated(It.IsAny<Order>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithValidId_UpdatesOrder()
    {
        // Arrange
        var newTitle = "Updated Title";
        var newStatus = OrderStatus.Approved;

        // Act
        var result = await _orderService.UpdateAsync("order-test", newTitle, null, newStatus);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be(newTitle);
        result.Status.Should().Be(newStatus);

        _notificationServiceMock.Verify(
            x => x.BroadcastOrderUpdated(It.IsAny<Order>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _orderService.UpdateAsync("non-existent", "Title", null, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesOrder()
    {
        // Act
        var result = await _orderService.DeleteAsync("order-test");

        // Assert
        result.Should().BeTrue();

        var deletedOrder = await _context.Orders.FindAsync("order-test");
        deletedOrder.Should().BeNull();

        _notificationServiceMock.Verify(
            x => x.BroadcastOrderDeleted("order-test"),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Act
        var result = await _orderService.DeleteAsync("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddItemAsync_AddsItemToOrder()
    {
        // Arrange
        var name = "New Item";
        var quantity = 5;
        var price = 25.00m;

        // Act
        var result = await _orderService.AddItemAsync("order-test", name, quantity, price);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Quantity.Should().Be(quantity);
        result.Price.Should().Be(price);
        result.OrderId.Should().Be("order-test");
    }

    [Fact]
    public async Task AddItemAsync_WithInvalidOrderId_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _orderService.AddItemAsync("non-existent", "Item", 1, 10));
    }

    [Fact]
    public async Task RemoveItemAsync_WithValidId_RemovesItem()
    {
        // Act
        var result = await _orderService.RemoveItemAsync("item-test");

        // Assert
        result.Should().BeTrue();

        var removedItem = await _context.OrderItems.FindAsync("item-test");
        removedItem.Should().BeNull();
    }

    [Fact]
    public async Task RemoveItemAsync_WithInvalidId_ReturnsFalse()
    {
        // Act
        var result = await _orderService.RemoveItemAsync("non-existent");

        // Assert
        result.Should().BeFalse();
    }
}
