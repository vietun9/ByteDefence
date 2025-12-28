# Developer Setup Guide

## Prerequisites

1. **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **Azure Functions Core Tools v4** - [Install Guide](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
3. **Docker Desktop** (optional) - [Download](https://www.docker.com/products/docker-desktop)
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
    "SignalR__Mode": "Local",
    "SignalR__HubUrl": "http://localhost:5000",
    "Jwt__Secret": "ByteDefence-Super-Secret-Key-For-Development-Only-32Chars!",
    "Jwt__Issuer": "ByteDefence",
    "Jwt__Audience": "ByteDefence-API"
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

## Troubleshooting

### Azure Functions won't start

1. Ensure Azure Functions Core Tools v4 is installed:
   ```bash
   func --version
   ```
2. Check that the local.settings.json file exists in the Api project

### SignalR connection fails

1. Verify the SignalR hub is running on port 5000
2. Check CORS settings in the SignalR project
3. Ensure the web app has the correct SignalR hub URL

### GraphQL returns authentication errors

1. Login first to get a JWT token
2. Ensure the Authorization header is set: `Bearer <token>`
3. Check that the token hasn't expired (8-hour validity)

## IDE Setup

### Visual Studio 2022

1. Open `ByteDefence.sln`
2. Set multiple startup projects: Api, SignalR, Web
3. Press F5 to start debugging

### VS Code

1. Install C# Dev Kit extension
2. Open the repository folder
3. Use the provided launch configurations or run manually
