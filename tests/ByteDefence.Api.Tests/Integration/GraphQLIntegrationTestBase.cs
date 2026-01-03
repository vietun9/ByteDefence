using ByteDefence.Api.Data;
using ByteDefence.Api.GraphQL;
using ByteDefence.Api.GraphQL.DataLoaders;
using ByteDefence.Api.GraphQL.Schema.Mutations;
using ByteDefence.Api.GraphQL.Schema.Queries;
using ByteDefence.Api.GraphQL.Schema.Types;
using ByteDefence.Api.Options;
using ByteDefence.Api.Services;
using ByteDefence.Api.Tests.Support;
using ByteDefence.Shared.Models;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ByteDefence.Api.Tests.Integration;

/// <summary>
/// Base class for GraphQL integration tests.
/// Sets up HotChocolate executor with in-memory database.
/// </summary>
public abstract class GraphQLIntegrationTestBase : IDisposable
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IRequestExecutor Executor;
    protected readonly AppDbContext DbContext;
    protected readonly IAuthService AuthService;

    protected GraphQLIntegrationTestBase()
    {
        var services = new ServiceCollection();

        // Add logging services
        services.AddLogging();

        // Configure in-memory configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:SigningKey", "Key-For-Development-Only-32Chars" },
                { "Jwt:Issuer", "ByteDefence" },
                { "Jwt:Audience", "ByteDefence-API" },
                { "Jwt:TokenLifetimeMinutes", "60" }
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        // Configure in-memory database with a fixed name per test class instance
        // Using ServiceLifetime.Singleton to share across all scopes
        var dbName = $"ByteDefence_Test_{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName), ServiceLifetime.Singleton);

        // Also register DbContextFactory for parallel query support
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName), ServiceLifetime.Singleton);

        // Register services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INotificationService, TestNotificationService>();

        // Configure HotChocolate GraphQL with DataLoaders
        services
            .AddGraphQLServer()
            .AddQueryType<OrderQueryResolver>()
            .AddMutationType()
            .AddTypeExtension<OrderMutationResolver>()
            .AddTypeExtension<AuthMutationResolver>()
            .AddType<OrderType>()
            .AddType<OrderItemType>()
            .AddType<UserType>()
            .AddDataLoader<UserByIdDataLoader>()
            .AddFiltering()
            .AddSorting()
            .AddErrorFilter<GraphQLErrorFilter>();

        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<AppDbContext>();
        AuthService = ServiceProvider.GetRequiredService<IAuthService>();

        // Ensure database is created and seed test data manually
        DbContext.Database.EnsureCreated();
        SeedTestData();

        // Get executor
        var executorResolver = ServiceProvider.GetRequiredService<IRequestExecutorResolver>();
        Executor = executorResolver.GetRequestExecutorAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Seeds test data manually to ensure proper BCrypt password hashing.
    /// </summary>
    private void SeedTestData()
    {
        // Check if data already exists (from EF seeding)
        if (DbContext.Users.Any())
        {
            return;
        }

        // Seed users with proper BCrypt hashed passwords
        var adminUser = new User
        {
            Id = "admin-001",
            Username = "admin",
            Email = "admin@bytedefence.com",
            PasswordHash = UserService.HashPassword("admin123"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        var regularUser = new User
        {
            Id = "user-001",
            Username = "user",
            Email = "user@bytedefence.com",
            PasswordHash = UserService.HashPassword("user123"),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Users.AddRange(adminUser, regularUser);

        // Seed orders
        var order1 = new Order
        {
            Id = "order-001",
            Title = "Office Supplies",
            Description = "Monthly office supplies order",
            Status = OrderStatus.Pending,
            CreatedById = adminUser.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var order2 = new Order
        {
            Id = "order-002",
            Title = "IT Equipment",
            Description = "New laptops for development team",
            Status = OrderStatus.Approved,
            CreatedById = adminUser.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var order3 = new Order
        {
            Id = "order-003",
            Title = "Training Materials",
            Description = "Books and courses for team training",
            Status = OrderStatus.Draft,
            CreatedById = regularUser.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };

        DbContext.Orders.AddRange(order1, order2, order3);

        // Seed order items
        var items = new[]
        {
            new OrderItem { Id = "item-001", OrderId = order1.Id, Name = "Notebooks", Quantity = 50, Price = 5.99m },
            new OrderItem { Id = "item-002", OrderId = order1.Id, Name = "Pens (Box)", Quantity = 20, Price = 12.50m },
            new OrderItem { Id = "item-003", OrderId = order1.Id, Name = "Sticky Notes", Quantity = 100, Price = 2.25m },
            new OrderItem { Id = "item-004", OrderId = order2.Id, Name = "MacBook Pro 14\"", Quantity = 5, Price = 2499.00m },
            new OrderItem { Id = "item-005", OrderId = order2.Id, Name = "External Monitor 27\"", Quantity = 5, Price = 449.00m },
            new OrderItem { Id = "item-006", OrderId = order3.Id, Name = "Clean Code Book", Quantity = 10, Price = 45.00m },
            new OrderItem { Id = "item-007", OrderId = order3.Id, Name = "Pluralsight Subscription", Quantity = 5, Price = 299.00m }
        };

        DbContext.OrderItems.AddRange(items);
        DbContext.SaveChanges();
    }

    /// <summary>
    /// Execute a GraphQL query/mutation without authentication.
    /// </summary>
    protected async Task<IOperationResult> ExecuteAsync(string query, Dictionary<string, object?>? variables = null)
    {
        var requestBuilder = OperationRequestBuilder.New()
            .SetDocument(query);

        if (variables != null)
        {
            requestBuilder.SetVariableValues(variables);
        }

        var result = await Executor.ExecuteAsync(requestBuilder.Build());
        return (IOperationResult)result;
    }

    /// <summary>
    /// Execute a GraphQL query/mutation with authentication.
    /// </summary>
    protected async Task<IOperationResult> ExecuteAuthenticatedAsync(string query, string userId, Dictionary<string, object?>? variables = null)
    {
        var requestBuilder = OperationRequestBuilder.New()
            .SetDocument(query)
            .SetGlobalState("CurrentUser", userId);

        // Fetch user from database to set role
        // We use GetAwaiter().GetResult() or Find() synchronously if we want, but FindAsync is better.
        // However, we are in an async method.
        var user = await DbContext.Users.FindAsync(userId);
        if (user != null)
        {
            requestBuilder.SetGlobalState("CurrentRole", user.Role.ToString());
        }

        if (variables != null)
        {
            requestBuilder.SetVariableValues(variables);
        }

        var result = await Executor.ExecuteAsync(requestBuilder.Build());
        return (IOperationResult)result;
    }

    /// <summary>
    /// Helper to extract data from result.
    /// </summary>
    protected IReadOnlyDictionary<string, object?>? GetData(IOperationResult result)
    {
        return result.Data;
    }

    /// <summary>
    /// Helper to check if result has errors.
    /// </summary>
    protected bool HasErrors(IOperationResult result)
    {
        return result.Errors != null && result.Errors.Count > 0;
    }

    /// <summary>
    /// Helper to get first error message.
    /// </summary>
    protected string? GetFirstErrorMessage(IOperationResult result)
    {
        return result.Errors?.FirstOrDefault()?.Message;
    }

    /// <summary>
    /// Helper to check if error message indicates an authentication/authorization error.
    /// </summary>
    protected bool IsAuthError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage)) return false;
        return errorMessage.Contains("Authentication") ||
               errorMessage.Contains("Unauthorized") ||
               errorMessage.Contains("authenticated") ||
               errorMessage.Contains("authorized");
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        (ServiceProvider as IDisposable)?.Dispose();
    }
}
