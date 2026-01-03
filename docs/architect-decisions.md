# Architect decisions
---

## 1. Blazor Flavor: WebAssembly (WASM)

**Decision:** **Blazor WebAssembly** hosted on **Azure Static Web Apps**.
**Reason:**
*   **Distribution**: Models a modern, decoupled SPA architecture.
*   **Scaling**: Offloads UI rendering to client; API scales independently.
*   **Hosting**: Azure Static Web Apps is highly cost-effective and integrates well with GitHub Actions.
*   **Security**: Enforces clear boundary between Client (Browser) and Server (API), requiring explicit token-based auth.

---

## 2. Data Model: Order Items

**Decision:** **Orders** with **Line Items**.
**Reason:**
*   **Complexity**: Sufficiently complex to show nested GraphQL resolvers (`Order.items`) and aggregate calculations (`Order.total`).
*   **Real-time**: Order status changes (e.g., "Placed" -> "Processing") are a perfect use case for push notifications.
*   **Concurrency**: Order stats dashboard allows measuring performance of parallel queries.

---

## 3. Auth Path: ASP.NET Core Integration (JWT Bearer)

**Decision:** **ASP.NET Core style JWT Bearer Authentication** with middleware and optional APIM/Gateway trust.

**Reason:**
*   **Flexibility**: Supports three deployment scenarios:
    1. **Full validation**: API validates JWT (default)
    2. **APIM trust**: APIM validates JWT, API trusts the token (`Auth:SkipJwtValidation=true`)
*   **Realism**: Uses real `HMACSHA256` signing and validation (not just a string check).
*   **Security**: 
    *   Secrets stored in **Azure Key Vault**.
    *   Function App accesses secret via **Managed Identity**.
*   **Configuration**: `Auth:SkipJwtValidation` setting allows switching between validation modes.
    *   APIM = coarse-grained auth (token validation, rate limits)
    *   API = fine-grained auth (roles, policies, business rules)

---

## 4. Real-Time Mechanism: SignalR Service

**Decision:** **Azure SignalR Service** (Serverless Mode). For local development, use **Self-hosted SignalR Hub** to simulate SignalR Service.
**Reason:**
*   **Scale**: Offloads connection management from the Function App.
*   **Architecture**: Fits the serverless model perfectly (Function triggers update -> SignalR Service broadcasts).


---

## 5. Infrastructure: Bicep + Key Vault

**Decision:** **Bicep** IaC with **Key Vault Integration**.
**Reason:**
*   **Security First**: No secrets in `local.settings.json` or code. All secrets (JWT key, connection strings) are injected into Key Vault at deployment time.
*   **IaC Choice**: Azure-native; No state file; manages state via the ARM template.
*   **Managed Identity**: Service-to-service authentication (Function -> Key Vault) is handled via identity, removing the need for secret rotation in app config.
*   **Reproducibility**: `infra/main.bicep` orchestrates the entire stack (Cosmos, SignalR, Function, SWA, Key Vault) in one command.

---

## 6. GraphQL Library: HotChocolate

**Decision:** **HotChocolate** (Server) + **StrawberryShake** (Client).
**Reason:**
*   **Ecosystem**: Best-in-class .NET GraphQL support.
*   **Features**: Built-in support for projections, filtering, and easy integration with Entity Framework.
*   **Client**: StrawberryShake generates strongly-typed C# client code, eliminating runtime typability errors.

---

## 7. Testing Strategy: Banana Cake Pop + Manual

**Decision:** **Banana Cake Pop** (IDE) and **Manual Scenarios**.
**Reason:**
*   **Exploration**: Banana Cake Pop is built into HotChocolate, enabling instant schema browsing.
*   **Verification**: `docs/testing.md` outlines manual steps to verify auth, concurrency, and error handling, which yields high confidence for this scope.
