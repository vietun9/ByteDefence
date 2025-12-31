# Deployment Guide

This guide explains how to deploy the application to Azure using Bicep.

## Prerequisites

1.  **Azure CLI**
2.  **Bicep CLI**
3.  **Active Azure Subscription**

## Architecture

The deployment provisions the following resources:
*   **Azure Functions**: Host for the GraphQL API (serverless).
*   **Azure Cosmos DB**: NoSQL database for Orders and Items.
*   **Azure SignalR Service**: Managed service for real-time WebSocket connections.
*   **Azure Key Vault**: Secure storage for secrets (JWT keys, connection strings).
*   **Azure Static Web Apps**: Host for the Blazor WASM frontend.
*   **Application Insights**: Monitoring and observability.

## Deployment Steps

### 1. Login to Azure

```bash
az login
az account set --subscription "<YOUR_SUBSCRIPTION_ID>"
```

### 2. Create a Resource Group

```bash
az group create --name ByteDefence-RG --location eastus
```

### 3. Deploy Infrastructure

The `infra/main.bicep` file orchestrates the entire deployment. You must provide a secure `jwtSecret` for token signing.

```bash
az deployment group create \
  --resource-group ByteDefence-RG \
  --template-file infra/main.bicep \
  --parameters jwtSecret="<YOUR_SECURE_RANDOM_STRING_AT_LEAST_32_CHARS>" \
  --parameters environment="dev"
```

*   **Tip**: For production, use `environment="prod"`.
*   **Tip**: The `jwtSecret` is automatically stored in Key Vault and referenced by the Function App.

### 4. Get Deployment Outputs

After successful deployment, the command outputs the URLs for your services. Note them down:

*   `functionAppUrl`: The GraphQL API Endpoint.
*   `staticWebAppUrl`: The Frontend URL.
*   `signalREndpoint`: (Internal usage, usually)
*   `keyVaultName`: Name of the created Key Vault.

### 5. Deployment Verification

1.  Navigate to `functionAppUrl/api/graphql` (via a tool like Banana Cake Pop).
2.  Try to login (this validates Key Vault access for the JWT secret).
3.  Navigate to `staticWebAppUrl`.
    *   **Note**: You may need to update the frontend configuration if it's not automatically wired up (CI/CD pipelines usually handle this).
    *   For manual validation, you might need to rebuild the Blazor app with the new API URL and deploy it to the Static Web App using `swa deploy` or GitHub Actions.

## CI/CD (Optional)

In a real scenario, you would set up a GitHub Action to:
1.  Run `az deployment group create`.
2.  Build the .NET projects.
3.  Deploy the Function App code (`func azure functionapp publish`).
4.  Deploy the Blazor WASM code to SWA (`swa deploy`).

## Security Notes

*   **Secrets**: No secrets are stored in code. They are passed as parameters during deployment and stored immediately in Key Vault.
*   **Access Control**: The Function App uses a System-Assigned Managed Identity to access Key Vault secrets securely.
