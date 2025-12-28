using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using ByteDefence.Web;
using ByteDefence.Web.Services;
using ByteDefence.Web.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add local storage
builder.Services.AddBlazoredLocalStorage();

// Add authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// Add GraphQL client
builder.Services.AddScoped<IGraphQLClient, GraphQLClient>();

// Add order service
builder.Services.AddScoped<IOrderService, OrderService>();

// Add SignalR service
builder.Services.AddSingleton<ISignalRService, SignalRService>();

await builder.Build().RunAsync();
