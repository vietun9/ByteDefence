using ByteDefence.Api.Data;
using ByteDefence.Api.GraphQL;
using ByteDefence.Api.GraphQL.DataLoaders;
using ByteDefence.Api.GraphQL.Schema.Mutations;
using ByteDefence.Api.GraphQL.Schema.Queries;
using ByteDefence.Api.GraphQL.Schema.Types;
using ByteDefence.Api.Middleware;
using ByteDefence.Api.Options;
using ByteDefence.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        // JWT Authentication middleware runs before function execution
        worker.UseMiddleware<JwtAuthenticationMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var environment = configuration["AZURE_FUNCTIONS_ENVIRONMENT"] ?? "Development";

        // Bind JWT options from configuration
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        // Validate JWT settings early to avoid starting with invalid auth configuration
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        ValidateJwtOptions(jwtOptions, environment);

        // Configure database with DbContextFactory for thread-safe parallel queries
        var useCosmosDb = configuration.GetValue<bool>("UseCosmosDb");
        if (useCosmosDb)
        {
            var connectionString = configuration.GetConnectionString("CosmosDb")
                ?? configuration["CosmosDb:ConnectionString"];
            var databaseName = configuration["CosmosDb:DatabaseName"] ?? "ByteDefence";

            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseCosmos(connectionString!, databaseName));
            services.AddDbContext<AppDbContext>(options =>
                options.UseCosmos(connectionString!, databaseName));
        }
        else
        {
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseInMemoryDatabase("ByteDefence"));
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("ByteDefence"));
        }

        // Register services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IAuthService, AuthService>();

        // Register notification service based on configuration
        var signalRMode = configuration["SignalR:Mode"] ?? "Local";
        if (signalRMode.Equals("Azure", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<INotificationService, AzureSignalRNotificationService>();
        }
        else
        {
            services.AddSingleton<INotificationService, LocalNotificationService>();
        }

        // Configure HotChocolate GraphQL with DataLoaders
        // Note: Authorization is handled via GlobalState pattern in resolvers
        // because Azure Functions isolated model doesn't support ASP.NET Core middleware stack
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

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

// Ensure database is created and seeded
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

await host.RunAsync();

static void ValidateJwtOptions(JwtOptions options, string environment)
{
    if (string.IsNullOrWhiteSpace(options.SigningKey))
    {
        throw new InvalidOperationException("Jwt:SigningKey must be configured. Use a strong secret for all environments.");
    }

    if (options.SigningKey.Length < 32)
    {
        throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters for HMAC signatures.");
    }

    if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
    {
        throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
    }
}
