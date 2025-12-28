# ByteDefence

Modern cloud-ready backend-frontend system with Azure Functions, GraphQL, and Blazor WebAssembly.

## Quick Start

### Prerequisites
- .NET 8 SDK
- Docker Desktop (optional, for containerized development)
- Azure Functions Core Tools v4 (for local Functions development)

### Run Locally (Without Docker)

```bash
# Clone the repository
git clone https://github.com/leviettung200/ByteDefence.git
cd ByteDefence

# Restore and build
dotnet restore
dotnet build

# Start the SignalR Hub (Terminal 1)
cd src/ByteDefence.SignalR
dotnet run

# Start the Azure Functions API (Terminal 2)
cd src/ByteDefence.Api
func start

# Start the Blazor Web App (Terminal 3)
cd src/ByteDefence.Web
dotnet run
```

### Run with Docker Compose

```bash
docker-compose up --build
```

### Access Points
| Service | URL |
|---------|-----|
| **Blazor App** | http://localhost:5001 |
| **GraphQL API** | http://localhost:7071/api/graphql |
| **SignalR Hub** | http://localhost:5000/hubs/notifications |

### Test Credentials
| Role  | Username | Password |
|-------|----------|----------|
| Admin | admin    | admin123 |
| User  | user     | user123  |

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Blazor WASM    │────▶│  Azure Functions│────▶│  EF Core        │
│  (Frontend)     │     │  + HotChocolate │     │  (In-Memory DB) │
└────────┬────────┘     └────────┬────────┘     └─────────────────┘
         │                       │
         │                       ▼
         │              ┌─────────────────┐
         └─────────────▶│  SignalR Hub    │
                        │  (Real-time)    │
                        └─────────────────┘
```

### Technology Stack
| Layer | Technology | Reason |
|-------|------------|--------|
| **API Runtime** | Azure Functions (Isolated) | Serverless, cost-effective, scales to zero |
| **GraphQL** | HotChocolate | Best .NET GraphQL library, excellent DX |
| **Real-time (Local)** | Self-hosted SignalR Hub | No Azure dependency for local dev |
| **Database (Local)** | EF Core In-Memory | Fast setup, no dependencies |
| **Frontend** | Blazor WASM | C# everywhere, SPA experience |
| **Auth** | JWT (mock) | Demonstrates pattern without complexity |
| **Infrastructure** | Bicep | Native Azure IaC, readable |

## Project Structure

```
ByteDefence/
├── src/
│   ├── ByteDefence.Api/           # Azure Functions + GraphQL
│   ├── ByteDefence.SignalR/       # Self-hosted SignalR Hub
│   ├── ByteDefence.Shared/        # Shared models/contracts
│   └── ByteDefence.Web/           # Blazor WASM Frontend
├── tests/
│   ├── ByteDefence.Api.Tests/
│   └── ByteDefence.Web.Tests/
├── infra/                         # Bicep templates
├── docs/
│   ├── schema.graphql             # Complete GraphQL schema
│   ├── setup.md                   # Developer setup guide
│   └── testing.md                 # Testing guide
└── docker-compose.yml
```

## GraphQL API

### Example Queries

**Login (Get JWT Token):**
```graphql
mutation {
  login(input: { username: "admin", password: "admin123" }) {
    token
    user { id username role }
    errorMessage
  }
}
```

**Get Orders (Requires Auth):**
```graphql
query {
  orders {
    id
    title
    status
    total
    items {
      name
      quantity
      price
      subtotal
    }
    createdBy {
      username
    }
  }
}
```

**Create Order:**
```graphql
mutation {
  createOrder(input: { title: "New Order", description: "Test order" }) {
    order {
      id
      title
      status
    }
    errorMessage
  }
}
```

### Testing with Postman/Banana Cake Pop

1. Send a `POST` request to `http://localhost:7071/api/graphql`
2. Set `Content-Type: application/json`
3. For authenticated requests, add header: `Authorization: Bearer <token>`

## Real-time Updates

The application uses SignalR for real-time updates:

- **OrderCreated**: Broadcast when a new order is created
- **OrderUpdated**: Broadcast when an order is modified
- **OrderDeleted**: Broadcast when an order is deleted

The Blazor frontend automatically subscribes to these events and shows update notifications.

## Authentication

### How It Works

1. User calls the `login` mutation with username/password
2. Server validates credentials and returns a JWT token
3. Client stores the token in localStorage
4. Client attaches the token to all subsequent GraphQL requests
5. Server validates the token and extracts user context

### Token Structure

```json
{
  "sub": "user-id",
  "unique_name": "username",
  "email": "user@example.com",
  "role": "Admin",
  "exp": 1234567890
}
```

### Authorization

- Most queries/mutations require authentication (return error if no token)
- Some mutations (like `deleteOrder`) are admin-only in the UI
- 401 errors are returned for missing/invalid tokens
- 403 errors are returned for insufficient permissions

## Documentation

- [Developer Setup](docs/setup.md)
- [Testing Guide](docs/testing.md)
- [GraphQL Schema](docs/schema.graphql)
- [Architecture Decisions](architect-decisions.md)
- [Implementation Plan](Implement-plan.md)

## Known Limitations

- Mock JWT authentication (not production-ready)
- In-memory database resets on restart (local only)
- No refresh token implementation
- SignalR hub is self-hosted (not Azure SignalR Service for local dev)

## Environment Configuration

| Setting | Local | Azure |
|---------|-------|-------|
| `SignalR:Mode` | Local | Azure |
| `SignalR:HubUrl` | http://localhost:5000 | Azure SignalR URL |
| `UseCosmosDb` | false | true |
| `Jwt:Secret` | dev-secret | Azure Key Vault |
| `Jwt:Issuer` | localhost | Azure URL |

## License

MIT
