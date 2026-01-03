# ByteDefence

Modern cloud-ready backend-frontend system with Azure Functions, GraphQL, and Blazor WebAssembly.

## Quick Start

For detailed local setup instructions, see [Developer Setup Guide](docs/setup.md).

### Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools v4

### 1-Minute Run
```bash
# Terminals 1, 2, 3
cd src/ByteDefence.SignalR && dotnet run
cd src/ByteDefence.Api && func start
cd src/ByteDefence.Web && dotnet run
```

Access:
- **Web App**: http://localhost:5001
- **API**: http://localhost:7071/api/graphql
- **SignalR**: http://localhost:5000

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

## Documentation

| Document | Description |
|----------|-------------|
| [Setup Guide](docs/setup.md) | detailed local development setup |
| [Testing Guide](docs/testing.md) | query examples, manual test steps, and Postman guide |
| [Deployment Guide](infra/deployment.md) | how to deploy to Azure using Bicep |
| [Architecture Decisions](docs/architect-decisions.md) | trade-offs (WASM vs Server, SignalR, etc.) |
| [AI Usage](docs/ai-usage.md) | summary of AI tools used in development |
| [GraphQL Schema](docs/schema.graphql) | complete API type definition |

## Project Structure

```
ByteDefence/
├── src/
│   ├── ByteDefence.Api/           # Azure Functions + GraphQL
│   ├── ByteDefence.SignalR/       # Self-hosted SignalR Hub (Local)
│   ├── ByteDefence.Shared/        # Shared DTOs
│   └── ByteDefence.Web/           # Blazor WASM Frontend
├── infra/                         # Bicep IaC & Deployment Docs
├── docs/                          # Developer documentation
└── tests/                         # Unit & Integration tests
```

## Key Features

*   **GraphQL API**: Full CRUD with HotChocolate.
*   **Real-time**: SignalR integration for live order updates.
*   **Security**: JWT Bearer Authentication (ASP.NET Core style middleware with APIM/Gateway trust option).
*   **Infrastructure**: Bicep templates for full Azure provisioning.
