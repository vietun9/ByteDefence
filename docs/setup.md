# Developer Setup Guide

## Prerequisites

1. **.NET 8 SDK**
2. **Azure Functions Core Tools v4**
3. **Docker Desktop** (optional)
4. **IDE** - Visual Studio 2022, VS Code, or JetBrains Rider

## Quick Setup

### 1. Clone the Repository

```bash
git clone https://github.com/leviettung200/ByteDefence.git
cd ByteDefence
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Solution

```bash
dotnet build
```

### 4. Run All Services

You need three terminals:

**Terminal 1 - SignalR Hub:**
```bash
cd src/ByteDefence.SignalR
dotnet run
```
The SignalR hub will start on `http://localhost:5000`

**Terminal 2 - Azure Functions API:**
```bash
cd src/ByteDefence.Api
func start
```
The GraphQL API will be available at `http://localhost:7071/api/graphql`

**Terminal 3 - Blazor Web App:**
```bash
cd src/ByteDefence.Web
dotnet run
```
The web app will be available at `http://localhost:5001` (or the port shown)

## Configuration

### API Configuration (local.settings.json)

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "UseCosmosDb": "false",
    "SignalR:Mode": "Local",
    "SignalR:HubUrl": "http://localhost:5000",
    "Jwt:Issuer": "ByteDefence",
    "Jwt:Audience": "ByteDefence-API",
    "Jwt:SigningKey": "Key-For-Development-Only-32Chars",
    "Jwt:TokenLifetimeMinutes": "60",
    "Auth:SkipJwtValidation": "false"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "http://localhost:8080,http://localhost:5001",
    "CORSCredentials": true
  }
}
```

### Web Configuration (wwwroot/appsettings.json)

```json
{
  "Api": {
    "Url": "http://localhost:7071/api/graphql"
  },
  "SignalR": {
    "HubUrl": "http://localhost:5000/hubs/notifications"
  }
}
```

## Docker Setup

### Build and Run with Docker Compose
```bash
docker-compose up --build
```

### Stop Services
```bash
docker-compose down
```

## Cloud Deployment

To deploy this application to Azure (including Key Vault, SignalR Service, and Functions), please refer to the **[Deployment Guide](../infra/deployment.md)**.

## Troubleshooting

- **Azure Functions won't start?** Verify `local.settings.json` exists in `src/ByteDefence.Api/`.
- **SignalR failing?** Ensure the hub is running cleanly on port 5000 in a separate terminal.
- **Auth errors?** Tokens expire after 8 hours. Re-login via the UI or Mutation to get a fresh token.

## Environment Variables

| Service | File | Key Variables |
|---------|------|---------------|
| **API** | `local.settings.json` | `Jwt:SigningKey`, `Jwt:Issuer`, `Jwt:Audience`, `SignalR:HubUrl`, `UseCosmosDb` |
| **Web** | `wwwroot/appsettings.json` | `Api:Url`, `SignalR:HubUrl` |

