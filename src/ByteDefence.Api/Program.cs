using ByteDefence.Api.Data;
using ByteDefence.Api.GraphQL;
using ByteDefence.Api.GraphQL.Schema.Mutations;
using ByteDefence.Api.GraphQL.Schema.Queries;
using ByteDefence.Api.GraphQL.Schema.Types;
using ByteDefence.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Configure database
        var useCosmosDb = configuration.GetValue<bool>("UseCosmosDb");
        if (useCosmosDb)
        {
            var connectionString = configuration.GetConnectionString("CosmosDb") 
                ?? configuration["CosmosDb:ConnectionString"];
            var databaseName = configuration["CosmosDb:DatabaseName"] ?? "ByteDefence";
            
            services.AddDbContext<AppDbContext>(options =>
                options.UseCosmos(connectionString!, databaseName));
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("ByteDefence"));
        }

        // Register services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INotificationService, LocalNotificationService>();

        // Configure HotChocolate GraphQL
        services
            .AddGraphQLServer()
            .AddQueryType<OrderQueryResolver>()
            .AddMutationType()
            .AddTypeExtension<OrderMutationResolver>()
            .AddTypeExtension<AuthMutationResolver>()
            .AddType<OrderType>()
            .AddType<OrderItemType>()
            .AddType<UserType>()
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
