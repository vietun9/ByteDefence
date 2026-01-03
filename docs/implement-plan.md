# 🚀 Delivery Plan:  Azure Functions GraphQL + Blazor WASM

## Overview

| Aspect | Decision |
|--------|----------|
| **Backend** | Azure Functions (Isolated Worker) + HotChocolate GraphQL |
| **Frontend** | Blazor WebAssembly (Standalone) |
| **Real-time (Local)** | Self-hosted SignalR Hub |
| **Real-time (Production)** | Azure SignalR Service |
| **Database (Local)** | EF Core In-Memory |
| **Database (Production)** | Azure Cosmos DB |
| **Auth** | Mock JWT (demo) → Azure AD B2C ready |
| **Infrastructure** | Bicep |

---

## 📁 Project Structure

```
ByteDefence/
├── src/
│   ├── ByteDefence.Api/                    # Azure Functions + GraphQL
│   │   ├── Functions/
│   │   │   ├── GraphQLFunction.cs          # HTTP trigger for GraphQL
│   │   │   └── SignalRFunctions.cs         # Azure SignalR bindings (prod)
│   │   ├── GraphQL/
│   │   │   ├── Schema/
│   │   │   │   ├── Types/                  # GraphQL types
│   │   │   │   ├── Queries/                # Query resolvers
│   │   │   │   ├── Mutations/              # Mutation resolvers
│   │   │   │   └── Subscriptions/          # If using GraphQL subscriptions
│   │   │   ├── Directives/                 # Auth directives
│   │   │   ├── Filters/                    # Error filters
│   │   │   └── Middleware/                 # Auth middleware
│   │   ├── Services/
│   │   │   ├── INotificationService.cs     # Abstraction
│   │   │   ├── LocalNotificationService.cs
│   │   │   └── AzureSignalRNotificationService.cs
│   │   ├── Data/                           # Repository + DbContext
│   │   └── Program.cs
│   │
│   ├── ByteDefence.SignalR/                # Self-hosted SignalR Hub (local)
│   │   ├── Hubs/
│   │   │   └── NotificationHub.cs
│   │   ├── Program.cs
│   │   └── Dockerfile
│   │
│   ├── ByteDefence. Shared/                 # Shared models/contracts
│   │   ├── Models/
│   │   └── DTOs/
│   │
│   └── ByteDefence.Web/                    # Blazor WASM
│       ├── Pages/
│       ├── Components/
│       ├── Services/
│       │   ├── GraphQLClient.cs            # GraphQL consumption
│       │   ├── AuthService.cs              # Token management
│       │   └── SignalRService.cs           # Real-time client
│       ├── Auth/
│       │   └── CustomAuthStateProvider.cs
│       └── Program.cs
│
├── tests/
│   ├── ByteDefence.Api.Tests/
│   │   ├── Unit/
│   │   └── Integration/
│   └── ByteDefence.Web.Tests/
│
├── infra/
│   ├── main.bicep                          # Azure infrastructure
│   ├── modules/
│   │   ├── functions. bicep
│   │   ├── signalr.bicep
│   │   ├── cosmos.bicep
│   │   └── staticwebapp.bicep
│   └── parameters/
│       ├── dev.parameters.json
│       └── prod.parameters. json
│
├── docs/
│   ├── decisions/                          # ADRs
│   ├── setup.md                            # Developer setup guide
│   ├── deployment.md                       # Deployment instructions
│   └── testing.md                          # Testing guide
│
├── . github/
│   └── workflows/
│       └── build. yml                       # Build + Test only
│
├── docker-compose.yml                      # Local development (API + SignalR + Web)
├── local.settings.json                     # Functions local config
└── README.md
```

---

## 📅 Phased Delivery Plan

### Phase 1: Foundation

| Task |
|------|
| Project scaffolding (solution + 4 projects) |
| Azure Functions setup with isolated worker |
| HotChocolate GraphQL integration |
| Basic schema (Order → Items) |
| Blazor WASM project setup |
| SignalR Hub project setup |
| Docker Compose for local development |

**Exit Criteria:**
- [ ] `docker-compose up` starts all services
- [ ] `func start` runs GraphQL endpoint locally
- [ ] Blazor app loads in browser
- [ ] Basic query works in Banana Cake Pop / GraphQL playground

---

### Phase 2: Core GraphQL

| Task |
|------|
| Complete schema design |
| Query resolvers with nested data |
| Mutation resolvers (Create, Update, Delete) |
| EF Core In-Memory data layer |
| Cosmos DB provider (production toggle) |
| Error filter implementation |
| Input validation |

**GraphQL Schema:**

```graphql
type Query {
  orders(first: Int, after:  String): OrderConnection! 
  order(id: ID!): Order
  me: User! 
}

type Mutation {
  createOrder(input: CreateOrderInput!): CreateOrderPayload! 
  updateOrder(input: UpdateOrderInput! ): UpdateOrderPayload!
  deleteOrder(id: ID! ): DeleteOrderPayload! 
  addOrderItem(input: AddOrderItemInput!): AddOrderItemPayload!
}

type Subscription {
  onOrderUpdated(orderId:  ID): Order! 
}

type Order {
  id:  ID!
  title: String!
  status: OrderStatus! 
  items: [OrderItem! ]!
  createdAt: DateTime! 
  updatedAt: DateTime!
  createdBy: User!
}

type OrderItem {
  id:  ID!
  name: String!
  quantity: Int! 
  price:  Decimal!
}

enum OrderStatus {
  DRAFT
  PENDING
  APPROVED
  COMPLETED
  CANCELLED
}
```

**Database Configuration:**

```csharp
// Program.cs
if (builder.Configuration. GetValue<bool>("UseCosmosDb"))
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseCosmos(
            connectionString,
            databaseName));
}
else
{
    services.AddDbContext<AppDbContext>(options =>
        options. UseInMemoryDatabase("ByteDefence"));
}
```

**Exit Criteria:**
- [ ] All CRUD operations work via GraphQL
- [ ] Nested queries resolve correctly
- [ ] Error responses have structured codes
- [ ] Database provider switches via config

---

### Phase 3: Authentication & Authorization

| Task |
|------|
| JWT token generation (mock) |
| Auth middleware for GraphQL |
| `@authorize` directive implementation |
| Role-based access (Admin, User) |
| Blazor AuthenticationStateProvider |
| Token storage & attachment |
| 401/403 handling on client |

**Auth Flow:**

```
┌─────────────┐     ┌─────────────────┐     ┌─────────────────┐
│ Blazor WASM │────▶│  Login Mutation │────▶│ JWT Token       │
└─────────────┘     └─────────────────┘     └─────────────────┘
       │                                            │
       │         ┌──────────────────────────────────┘
       ▼         ▼
┌─────────────────────┐     ┌─────────────────┐
│ Store in            │────▶│ Attach Bearer   │
│ localStorage        │     │ to requests     │
└─────────────────────┘     └─────────────────┘
```

**Exit Criteria:**
- [ ] Login returns valid JWT
- [ ] Authenticated queries work with token
- [ ] Unauthorized requests return 401
- [ ] Forbidden requests return 403
- [ ] Blazor shows auth state correctly

---

### Phase 4: Real-time Updates

| Task |
|------|
| Self-hosted SignalR Hub implementation |
| Notification service abstraction (`INotificationService`) |
| Local notification service implementation |
| Azure SignalR notification service implementation |
| SignalR mode switching (Local vs Azure) |
| Azure SignalR Service setup (production) |
| SignalR Azure Function triggers (production) |
| Broadcast on mutations |
| Blazor SignalR client with dynamic hub URL |
| UI auto-refresh on updates |
| Connection state handling |
| Docker Compose with SignalR service |

**Real-time Architecture (Local):**

```
┌─────────────┐     ┌─────────────────────────────────────┐
│ Blazor WASM │────▶│  Azure Functions (Local)            │
│             │     │  └── GraphQL Function               │
└──────┬──────┘     └──────────────────┬──────────────────┘
       │                               │
       │         ┌─────────────────────┘
       │         │ Broadcast via HTTP
       ▼         ▼
┌─────────────────────┐
│ SignalR Hub         │
│ (Self-hosted)       │
│ localhost: 5000      │
└─────────────────────┘
```

**Real-time Architecture (Production):**

```
┌─────────────┐     ┌─────────────────┐     ┌─────────────────┐
│ Blazor WASM │────▶│ Azure Functions │────▶│ Azure SignalR   │
│             │◀────│                 │◀────│ Service         │
└─────────────┘     └─────────────────┘     └─────────────────┘
```

**Notification Service Abstraction:**

```csharp
public interface INotificationService
{
    Task BroadcastOrderUpdated(Order order);
    Task BroadcastOrderCreated(Order order);
    Task BroadcastOrderDeleted(string orderId);
}

// Local implementation
public class LocalNotificationService :  INotificationService
{
    private readonly HttpClient _httpClient;
    
    public LocalNotificationService(IConfiguration config)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(config["SignalR:HubUrl"])
        };
    }
    
    public async Task BroadcastOrderUpdated(Order order)
    {
        await _httpClient.PostAsJsonAsync("/api/broadcast", new
        {
            Method = "OrderUpdated",
            Group = $"order-{order.Id}",
            Data = order
        });
    }
}

// Azure implementation
public class AzureSignalRNotificationService : INotificationService
{
    private readonly IHubContext _hubContext;
    
    public async Task BroadcastOrderUpdated(Order order)
    {
        await _hubContext.Clients
            .Group($"order-{order. Id}")
            .SendAsync("OrderUpdated", order);
    }
}
```

**SignalR Hub (Self-hosted):**

```csharp
public class NotificationHub : Hub
{
    public async Task JoinOrderGroup(string orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }

    public async Task LeaveOrderGroup(string orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }
}
```

**Docker Compose:**

```yaml
version: '3.8'
services:
  api:
    build:  ./src/ByteDefence.Api
    ports:
      - "7071:80"
    environment: 
      - SignalR__Mode=Local
      - SignalR__HubUrl=http://signalr: 5000
      - UseCosmosDb=false
    depends_on:
      - signalr

  signalr: 
    build: ./src/ByteDefence.SignalR
    ports: 
      - "5000:5000"

  web:
    build:  ./src/ByteDefence.Web
    ports:
      - "5000:80"
    environment:
      - SignalR__HubUrl=http://localhost:5000/hubs/notifications
      - Api__Url=http://localhost:7071/api/graphql
    depends_on:
      - api
      - signalr
```

**Blazor Client Configuration:**

```csharp
// Program.cs (Blazor)
var signalRHubUrl = builder.Configuration["SignalR: HubUrl"]
    ??  "http://localhost:5000/hubs/notifications";

builder. Services.AddScoped(sp =>
    new HubConnectionBuilder()
        .WithUrl(signalRHubUrl, options =>
        {
            options.AccessTokenProvider = async () =>
            {
                var authService = sp.GetRequiredService<IAuthService>();
                return await authService.GetTokenAsync();
            };
        })
        .WithAutomaticReconnect()
        .Build());
```

**Exit Criteria:**
- [ ] `docker-compose up` starts all services including SignalR
- [ ] Changes from one browser appear on another (locally)
- [ ] Same code works on Azure with SignalR Service
- [ ] Reconnection works after disconnect
- [ ] Connection state shown in UI

---

### Phase 5: Frontend Implementation

| Task |
|------|
| GraphQL client service |
| Order list page |
| Order detail page (nested items) |
| Create/Edit order forms |
| Loading states |
| Error handling UI |
| Toast notifications |
| Real-time update indicators |

**Page Structure:**

```
/                       → Dashboard (order summary)
/login                  → Login page
/orders                 → Order list (paginated)
/orders/{id}            → Order detail + items
/orders/new             → Create order
/orders/{id}/edit       → Edit order
```

**SignalR Integration in Pages:**

```csharp
@page "/orders/{Id}"
@inject HubConnection HubConnection
@implements IAsyncDisposable

<h1>Order:  @order?. Title</h1>
<ConnectionStatus State="@connectionState" />

@if (isUpdatedRemotely)
{
    <div class="alert alert-info">
        This order was updated.  <button @onclick="Refresh">Refresh</button>
    </div>
}

@code {
    [Parameter] public string Id { get; set; }
    
    private Order?  order;
    private HubConnectionState connectionState;
    private bool isUpdatedRemotely;

    protected override async Task OnInitializedAsync()
    {
        await HubConnection.StartAsync();
        await HubConnection.InvokeAsync("JoinOrderGroup", Id);
        
        HubConnection.On<Order>("OrderUpdated", (updated) =>
        {
            isUpdatedRemotely = true;
            StateHasChanged();
        });
        
        await LoadOrder();
    }

    public async ValueTask DisposeAsync()
    {
        await HubConnection.InvokeAsync("LeaveOrderGroup", Id);
    }
}
```

**Exit Criteria:**
- [ ] All pages functional
- [ ] Loading spinners during fetch
- [ ] Error messages displayed
- [ ] Forms validate before submit
- [ ] Real-time updates show notification

---

### Phase 6: Testing

| Task |
|------|
| Unit tests for resolvers |
| Unit tests for services |
| Integration tests for GraphQL |
| Auth scenario tests |
| SignalR integration tests |
| Blazor component tests |

**Test Structure:**

```
tests/
├── ByteDefence. Api.Tests/
│   ├── Unit/
│   │   ├── Resolvers/
│   │   │   ├── OrderQueryResolverTests.cs
│   │   │   └── OrderMutationResolverTests. cs
│   │   └── Services/
│   │       ├── OrderServiceTests.cs
│   │       └── NotificationServiceTests.cs
│   └── Integration/
│       ├── GraphQL/
│       │   ├── QueryIntegrationTests.cs
│       │   └── MutationIntegrationTests. cs
│       └── Auth/
│           ├── AuthenticationTests.cs
│           └── AuthorizationTests.cs
│
└── ByteDefence.Web.Tests/
    └── Components/
        └── OrderListTests.cs
```

**Example Tests:**

```csharp
// Unit Test - Resolver
public class OrderQueryResolverTests
{
    [Fact]
    public async Task GetOrder_WithValidId_ReturnsOrderWithItems()
    {
        // Arrange
        var mockService = new Mock<IOrderService>();
        mockService.Setup(s => s.GetByIdAsync("1"))
            .ReturnsAsync(new Order { Id = "1", Items = new List<OrderItem>() });
        
        var resolver = new OrderQueryResolver(mockService.Object);
        
        // Act
        var result = await resolver.GetOrderAsync("1");
        
        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
    }
}

// Integration Test - Auth
public class AuthenticationTests :  IClassFixture<TestServerFixture>
{
    [Fact]
    public async Task Query_WithoutToken_Returns401()
    {
        // Arrange
        var client = _fixture.CreateClient();
        
        // Act
        var response = await client.PostGraphQL("""
            query { me { id } }
        """);
        
        // Assert
        response. Errors.Should().Contain(e => e.Code == "UNAUTHENTICATED");
    }
    
    [Fact]
    public async Task Query_WithValidToken_ReturnsData()
    {
        // Arrange
        var client = _fixture.CreateAuthenticatedClient(role: "User");
        
        // Act
        var response = await client. PostGraphQL("""
            query { me { id name } }
        """);
        
        // Assert
        response. Data.Should().NotBeNull();
    }
}
```

**Exit Criteria:**
- [ ] All tests pass
- [ ] Build pipeline runs tests
- [ ] Auth scenarios covered
- [ ] Real-time scenarios covered

---

### Phase 7: Infrastructure & Deployment

| Task |
|------|
| Bicep templates for all resources |
| GitHub Actions build pipeline (build + test only) |
| Environment configuration (local/dev/prod) |
| Manual deployment scripts |
| Deployment documentation |
| Smoke tests post-deploy |

**Azure Resources:**

```
Resource Group:  rg-bytedefence-{env}
├── Azure Functions:         func-bytedefence-{env}
├── SignalR Service:        sigr-bytedefence-{env}
├── Cosmos DB:              cosmos-bytedefence-{env}
├── Static Web App:         swa-bytedefence-{env}
└── Application Insights:   appi-bytedefence-{env}
```

**Bicep Template (main.bicep):**

```bicep
param environment string
param location string = resourceGroup().location

module functions 'modules/functions.bicep' = {
  name: 'functions'
  params: {
    name: 'func-bytedefence-${environment}'
    location: location
    signalRConnectionString: signalr.outputs.connectionString
    cosmosConnectionString: cosmos.outputs.connectionString
  }
}

module signalr 'modules/signalr.bicep' = {
  name: 'signalr'
  params: {
    name: 'sigr-bytedefence-${environment}'
    location: location
  }
}

module cosmos 'modules/cosmos.bicep' = {
  name: 'cosmos'
  params: {
    name: 'cosmos-bytedefence-${environment}'
    location:  location
  }
}

module staticwebapp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp'
  params: {
    name: 'swa-bytedefence-${environment}'
    location:  location
    apiUrl: functions.outputs.url
  }
}
```

**GitHub Actions (build.yml):**

```yaml
name: Build and Test

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with: 
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run:  dotnet restore
      
      - name:  Build
        run:  dotnet build --no-restore
      
      - name: Test
        run: dotnet test --no-build --verbosity normal
      
      - name:  Publish API
        run: dotnet publish src/ByteDefence.Api -c Release -o ./publish/api
      
      - name: Publish Web
        run: dotnet publish src/ByteDefence.Web -c Release -o ./publish/web
      
      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with: 
          name: publish
          path: ./publish
```

**Manual Deployment Commands:**

```bash
# 1. Deploy infrastructure
az deployment group create \
  --resource-group rg-bytedefence-dev \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.parameters.json

# 2. Deploy Azure Functions
cd src/ByteDefence.Api
func azure functionapp publish func-bytedefence-dev

# 3. Deploy Blazor WASM to Static Web App
cd src/ByteDefence.Web
dotnet publish -c Release
az staticwebapp deploy \
  --name swa-bytedefence-dev \
  --app-location ./bin/Release/net8.0/publish/wwwroot

# 4. Verify deployment
curl https://func-bytedefence-dev.azurewebsites.net/api/graphql \
  -X POST \
  -H "Content-Type:  application/json" \
  -d '{"query":  "{ __typename }"}'
```

**Exit Criteria:**
- [ ] `az deployment` succeeds
- [ ] App accessible via Azure URL
- [ ] Build pipeline green
- [ ] Logs visible in Application Insights
- [ ] Real-time works via Azure SignalR Service

---

### Phase 8: Documentation & Polish

| Task |
|------|
| README with quick start |
| Developer setup guide |
| API documentation (GraphQL schema) |
| ADR documents |
| Deployment guide |
| Testing guide |
| Known limitations doc |
| Environment configuration guide |

**README Structure:**

````markdown name=README.md
# ByteDefence

Modern cloud-ready backend-frontend system with Azure Functions, GraphQL, and Blazor WASM.

## Quick Start

### Prerequisites
- . NET 8 SDK
- Docker Desktop
- Azure Functions Core Tools (optional for non-Docker)

### Run Locally

```bash
# Clone and start
git clone https://github.com/leviettung200/ByteDefence.git
cd ByteDefence
docker-compose up
```

Access: 
- **Blazor App**: http://localhost:5001
- **GraphQL Playground**: http://localhost:7071/api/graphql
- **SignalR Hub**: http://localhost:5000

### Test Credentials
| Role  | Username | Password |
|-------|----------|----------|
| Admin | admin    | admin123 |
| User  | user     | user123  |

## Architecture

[Architecture diagram]

## Documentation

- [Developer Setup](docs/setup.md)
- [Deployment Guide](docs/deployment.md)
- [Testing Guide](docs/testing.md)
- [Architecture Decisions](docs/decisions/)

## Known Limitations

- Mock JWT authentication (not production-ready)
- In-memory database resets on restart (local only)
- No refresh token implementation
```
````

**ADR Documents to Create:**

| ADR | Title |
|-----|-------|
| ADR-001 | Mock JWT vs Real OAuth |
| ADR-002 | EF Core In-Memory vs Cosmos DB |
| ADR-003 | Self-hosted SignalR vs Azure SignalR Service |
| ADR-004 | Blazor WASM vs Server |
| ADR-005 | Manual Deployment vs Full CI/CD |

**Exit Criteria:**
- [ ] New developer can run locally in < 15 min
- [ ] Deployment is repeatable
- [ ] Trade-offs clearly documented
- [ ] All ADRs written

---

## 🛠️ Technology Stack Summary

| Layer | Technology | Reason |
|-------|------------|--------|
| **API Runtime** | Azure Functions (Isolated) | Serverless, cost-effective, scales to zero |
| **GraphQL** | HotChocolate | Best . NET GraphQL library, excellent DX |
| **Real-time (Local)** | Self-hosted SignalR Hub | No Azure dependency for local dev |
| **Real-time (Prod)** | Azure SignalR Service | Managed, scales automatically |
| **Database (Local)** | EF Core In-Memory | Fast setup, no dependencies |
| **Database (Prod)** | Azure Cosmos DB | Scalable, serverless option |
| **Frontend** | Blazor WASM | C# everywhere, SPA experience |
| **GraphQL Client** | StrawberryShake | Type-safe, HotChocolate ecosystem |
| **Auth** | JWT (mock) | Demonstrates pattern without complexity |
| **Infrastructure** | Bicep | Native Azure IaC, readable |
| **CI** | GitHub Actions | Build + test on PR |
| **CD** | Manual | Azure CLI scripts |
| **Observability** | Application Insights | Azure-native, easy setup |

---

## 📋 Definition of Done

### Per Feature:
- [ ] Code reviewed (or self-reviewed with checklist)
- [ ] Unit tests written and passing
- [ ] Integration test for critical path
- [ ] Error handling implemented
- [ ] Logging added for key operations
- [ ] Documentation updated

### Project Complete:
- [ ] End-to-end flow works (login → CRUD → real-time → logout)
- [ ] Works locally via Docker Compose
- [ ] Deployed to Azure and accessible
- [ ] Real-time works in both environments
- [ ] Documentation complete
- [ ] All trade-offs documented
- [ ] Build pipeline functional

---

## ⚠️ Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| HotChocolate + Functions complexity | Start with HTTP trigger, follow official docs |
| SignalR self-hosted + Azure switching | Abstract via `INotificationService`, test both early |
| EF Core In-Memory vs Cosmos differences | Document limitations, test critical paths with both |
| Blazor WASM bundle size | Enable trimming, lazy load |
| Docker networking issues | Use docker-compose networks, document troubleshooting |

---

## 🔄 Environment Configuration Summary

| Setting | Local | Development | Production |
|---------|-------|-------------|------------|
| `SignalR: Mode` | Local | Azure | Azure |
| `SignalR:HubUrl` | http://localhost:5000 | Azure URL | Azure URL |
| `UseCosmosDb` | false | true | true |
| `Jwt:SigningKey` | dev secret | env variable | env variable |
| `Jwt:Issuer` | localhost | Azure URL | Azure URL |
| `Jwt:Audience` | byte defence | byte defence | byte defence |
| `Jwt:TokenLifetimeMinutes` | 60 | 60 | 60 |
| `CosmosDb:ConnectionString` | N/A | env variable | env variable |
| `UseCosmosDb` | false | true | true |
| `Auth:SkipJwtValidation` | false | false | false |