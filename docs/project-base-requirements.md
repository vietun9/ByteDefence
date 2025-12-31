## Azure GraphQL + Blazor Open-Book Exercise  

We encourage you to use AI coding tools; if you do, include a short summary of the prompts/conversations you used (or a transcript/export) so we can understand how they influenced the solution.  

### What You Will Build  
- A fully functional Azure‑hosted (or fully local) GraphQL application with CRUD and real‑time update support (GraphQL subscriptions or an equivalent mechanism), implemented in **C# 10+**.  
- A Blazor frontend (WASM or Server) must consume the GraphQL API. Mock responses are fine.  
- Explicitly state whether you chose **Blazor WebAssembly** or **Blazor Server** and why (trade‑offs, hosting fit, auth/client needs).  

### Suggested Flow  
1. Pick a data model (books, tasks, events, courses/lessons, orders/items, blog/comments, or a similar nested model).  
2. Stand up the GraphQL API with mock CRUD + real‑time support (subscriptions or SignalR/push).  
3. Build the Blazor UI that exercises the API (list/detail/create/edit + live updates).  
4. Add IaC for Azure deployment and wire auth (optional; local‑only is acceptable—Azure deployment earns extra credit).  
5. Document how to run, deploy (or run locally), and test (queries, mutations, real‑time updates).  

### Sample Models and Schema Hints  
- **Books** with nested authors and reviews (`Book { id, title, author: Author, reviews: [Review] }`).  
- **Tasks** with subtasks and tags (`Task { id, title, status, subtasks: [Task], tags: [Tag] }`).  
- **Courses** with lessons and instructors (`Course { id, title, lessons: [Lesson], instructor: Instructor }`).  
- **Orders** with line items (`Order { id, status, items: [OrderItem], total }`).  
- **Blog posts** with comments (`Post { id, title, content, comments: [Comment] }`).  

Keep IDs as strings/guids, include timestamps, and model basic enums (e.g., status). Show how nested children load (e.g., resolve Author, Reviews). Mock data is fine; keep the schema complete and typed.  

### Architecture and Tech Stack  
- **Language:** C# (10 or later).  
- **Frontend:** Blazor WebAssembly (Azure Static Web Apps) or Blazor Server (App Service); no Azure resource deployment is required for the UI—running locally is fine.  
- **Backend:** GraphQL hosted via Azure Functions (preferred—even when running locally) or Azure Container Apps; APIM/App Service gateway optional for cloud deployments (not required for local runs).  
- **Infrastructure as Code:** Bicep, Terraform, or Azure Developer CLI (AZD) with Dev/Test/Prod support.  

### Backend Requirements  
- Full GraphQL CRUD plus a real‑time mechanism (GraphQL subscriptions recommended; SignalR/push alternatives acceptable) returning mock data.  
- Include a complete schema file.  
- Use EF Core (in‑memory provider is fine or use SQLite) for data access abstractions; seed mock data and demonstrate basic query/update patterns through the `DbContext`.  
- Demonstrate the concurrent database queries.  
- At least one custom resolver (Function handler or middle‑layer logic).  
- Authorization rules enforced in the schema (custom directives acceptable).  
- **Auth:** protect endpoints with a bearer token (JWT or static/mock). For JWT (AAD/B2C/custom), you may mock validation locally; document token acquisition/validation. For static tokens, show header attachment and 401/403 handling.  
- Demonstrate error handling for at least one endpoint (mock error payload is fine).  
- Provide clear instructions for testing with Postman, Banana Cake Pop, APIM Test Console, or GraphQL Playground.  

### Frontend Requirements  
- Consume all GraphQL endpoints and show CRUD UI for the chosen model.  
- Show real‑time updates (GraphQL subscriptions or an equivalent mechanism such as SignalR/push notifications).  
- **Required pages:** list, detail, create, and edit.  
- **Required UX:** loading states, error states, and environment‑based API endpoint selection.  
- Use a strongly typed GraphQL client (e.g., StrawberryShake, GraphQL.Client).  
- **If Blazor WASM:** attach a bearer token (JWT or static/mock); MSAL for Azure AD is optional.  
- **If Blazor Server:** attach a bearer token (JWT or static/mock) or use Azure AD on‑behalf‑of flow; integrate SignalR or WebSocket subscription streaming.  

### Infrastructure and Deployment  
- Provision with Bicep/Terraform/AZD (optional; local‑only is acceptable—Azure deployment earns extra credit).  
- **If deploying to Azure:** Function App (GraphQL) is recommended; APIM/App Service gateway is optional.  
- Optional: Static Web Apps (for WASM), SignalR (if you choose that for real‑time), Key Vault, CI/CD pipelines (GitHub Actions or Azure DevOps). The Blazor app may run locally (no Azure deploy required).  

If deploying to Azure, parameterize for multiple environments and include authentication configuration; otherwise, document local configuration.  

If deploying to Azure, provide a single‑command redeploy to a different Azure subscription/account (e.g., one `azd up`, `bicep/terraform apply`, or scripted `az deployment` invocation with documented parameters).  

### Authentication Options  
- Bearer tokens only: JWT (AAD/B2C/custom issuer) or static/mock tokens are acceptable.  
- Show where validation happens (gateway/policy or function middleware) and how the client attaches the header. APIM policies are optional.  

### Deliverables  
- Repository/zip containing: schema, Function resolvers, infrastructure files (or local run notes), Blazor frontend, setup/install steps, and subscription/real‑time testing steps.  
- Either a deployed, working Azure endpoint (we provide the subscription) or clear instructions to run everything locally end‑to‑end.  
- Auth credentials or token guidance for testing.  

### Evaluation Criteria  
- Implementation completeness and correctness (CRUD + real‑time updates).  
- Code quality and structure.  
- Proper use of Azure Functions and (if chosen) IaC; APIM optional. Azure deployment earns extra credit but is not required for full credit.  
- Authentication integration and error handling.  
- Working Blazor frontend that exercises the API.  
- Clarity and completeness of documentation.