using BookStore.BlazorClient;
using BookStore.BlazorClient.Services;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Get API URL from configuration or use default for local development
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:7071/api";

// Register authentication services
builder.Services.AddSingleton<AuthTokenService>();
builder.Services.AddScoped<AuthHeaderHandler>();

// Configure HttpClient with auth header
builder.Services.AddScoped(sp =>
{
    var authTokenService = sp.GetRequiredService<AuthTokenService>();
    var handler = new AuthHeaderHandler(authTokenService)
    {
        InnerHandler = new HttpClientHandler()
    };
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
});

// Configure GraphQL client
builder.Services.AddScoped<IGraphQLClient>(sp =>
{
    var authTokenService = sp.GetRequiredService<AuthTokenService>();
    var handler = new AuthHeaderHandler(authTokenService)
    {
        InnerHandler = new HttpClientHandler()
    };
    var httpClient = new HttpClient(handler);
    
    var graphQLEndpoint = $"{apiBaseUrl}/graphql";
    return new GraphQLHttpClient(graphQLEndpoint, new SystemTextJsonSerializer(), httpClient);
});

builder.Services.AddScoped<BookStoreGraphQLService>();

await builder.Build().RunAsync();
