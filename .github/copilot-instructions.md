# AI Coding Agent Instructions

- Scope: Build the open-book exercise defined in [project-base-requirements.md](../project-base-requirements.md): Azure-hosted (or fully local) GraphQL API in C# 10+ plus a Blazor frontend (WASM or Server) consuming it.
- Track AI usage: record a brief summary of prompts/conversations used while coding.

## Architecture & Tech Choices
- Backend: Azure Functions (preferred) or Container Apps hosting GraphQL; EF Core (in-memory or SQLite) with seeded mock data and concurrent query examples; include a complete schema file; at least one custom resolver; enforce auth rules in schema/directives.
- Real-time: GraphQL subscriptions preferred; SignalR/push acceptable—document chosen mechanism and client wiring.
- Auth: Bearer token (JWT or static/mock). Show where validation happens (Function middleware/gateway) and how clients attach headers; document token acquisition/validation.
- Frontend: Blazor WebAssembly (Static Web Apps) or Blazor Server—state choice and rationale. Use strongly typed GraphQL client (e.g., StrawberryShake or GraphQL.Client). Provide required pages: list, detail, create, edit; include loading/error states and environment-based API selection.
- IaC: Bicep/Terraform/AZD optional but favored; parameterize environments when present; include single-command deploy/redeploy if Azure is supported.

## Required Behaviors
- CRUD endpoints plus real-time updates; include mock error handling example.
- IDs as string/guid; timestamps; enums for statuses; show nested child loading (e.g., author/reviews) in resolvers.
- Protect endpoints with bearer auth; return 401/403 appropriately; document testing headers.
- Provide testing instructions for GraphQL tooling (Postman, Banana Cake Pop, Playground, APIM console, etc.).

## Frontend Expectations
- Consume all GraphQL CRUD endpoints; show live updates via subscriptions/SignalR.
- Implement loading/error states; support environment-specific endpoints and token attachment.
- If WASM: note token attachment/MSAL optional. If Server: support on-behalf-of/AAD optional; integrate SignalR or subscription streaming.

## Delivery & Docs
- Include schema, Function resolvers, infrastructure (if any), Blazor app, setup/install steps, and subscription/real-time testing steps.
- If Azure deployment is provided, document parameters and a single-command redeploy path; otherwise, provide clear local run instructions end-to-end.
- Auth credentials or token guidance must be documented for testing.

## Work Style for Agents
- Prefer clear, minimal code with brief comments only where logic is non-obvious; keep to ASCII unless existing files require otherwise.
- Do not remove user changes; avoid destructive git commands.
- When unsure about requirements, consult [project-base-requirements.md](../project-base-requirements.md) and ask the user before deviating.
