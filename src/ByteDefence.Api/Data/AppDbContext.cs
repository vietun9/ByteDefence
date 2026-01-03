using ByteDefence.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ByteDefence.Api.Data;

public class AppDbContext : DbContext
{
    // Pre-computed BCrypt hashes for seed data (work factor: 12)
    // admin123 hash
    private const string AdminPasswordHash = "$2b$12$XtyPc7IO7nQh6Qnnp777YOUbRKIym4rhZrp4kUxRAsj7K5czt4muG";
    // user123 hash
    private const string UserPasswordHash = "$2b$12$a9/Jsr2WrMFHbOv8/XU7cuqZE49ylC6iX.eiTuAQD1ObYjW52BsvO";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasOne(e => e.CreatedBy)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(e => e.CreatedById);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(e => e.OrderId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Username).IsUnique();
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var adminId = "admin-001";
        var userId = "user-001";

        // Use fixed dates for deterministic seed data (avoids test flakiness)
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = adminId,
                Username = "admin",
                Email = "admin@bytedefence.com",
                PasswordHash = AdminPasswordHash,
                Role = UserRole.Admin,
                CreatedAt = baseDate
            },
            new User
            {
                Id = userId,
                Username = "user",
                Email = "user@bytedefence.com",
                PasswordHash = UserPasswordHash,
                Role = UserRole.User,
                CreatedAt = baseDate
            }
        );

        var order1Id = "order-001";
        var order2Id = "order-002";
        var order3Id = "order-003";

        modelBuilder.Entity<Order>().HasData(
            new Order
            {
                Id = order1Id,
                Title = "Office Supplies",
                Description = "Monthly office supplies order",
                Status = OrderStatus.Pending,
                CreatedById = adminId,
                CreatedAt = baseDate.AddDays(-5),
                UpdatedAt = baseDate.AddDays(-5)
            },
            new Order
            {
                Id = order2Id,
                Title = "IT Equipment",
                Description = "New laptops for development team",
                Status = OrderStatus.Approved,
                CreatedById = adminId,
                CreatedAt = baseDate.AddDays(-10),
                UpdatedAt = baseDate.AddDays(-3)
            },
            new Order
            {
                Id = order3Id,
                Title = "Training Materials",
                Description = "Books and courses for team training",
                Status = OrderStatus.Draft,
                CreatedById = userId,
                CreatedAt = baseDate.AddDays(-2),
                UpdatedAt = baseDate.AddDays(-2)
            }
        );

        modelBuilder.Entity<OrderItem>().HasData(
            new OrderItem { Id = "item-001", OrderId = order1Id, Name = "Notebooks", Quantity = 50, Price = 5.99m },
            new OrderItem { Id = "item-002", OrderId = order1Id, Name = "Pens (Box)", Quantity = 20, Price = 12.50m },
            new OrderItem { Id = "item-003", OrderId = order1Id, Name = "Sticky Notes", Quantity = 100, Price = 2.25m },
            new OrderItem { Id = "item-004", OrderId = order2Id, Name = "MacBook Pro 14\"", Quantity = 5, Price = 2499.00m },
            new OrderItem { Id = "item-005", OrderId = order2Id, Name = "External Monitor 27\"", Quantity = 5, Price = 449.00m },
            new OrderItem { Id = "item-006", OrderId = order3Id, Name = "Clean Code Book", Quantity = 10, Price = 45.00m },
            new OrderItem { Id = "item-007", OrderId = order3Id, Name = "Pluralsight Subscription", Quantity = 5, Price = 299.00m }
        );
    }
}
