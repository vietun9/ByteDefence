# Architect decisions
---

## 1. Blazor Flavor: WebAssembly vs Server

**Evaluation criteria:** delivery speed, hosting fit, security risk, real-time complexity, scalability, demo clarity

| Criteria                       | Blazor WebAssembly (WASM)                | Blazor Server                    |
| ------------------------------ | ---------------------------------------- | -------------------------------- |
| Hosting fit                    | Excellent with **Azure Static Web Apps** | Natural fit with **App Service** |
| Client/server boundary clarity | Very clear (SPA + API)                   | Blurred (server-side UI logic)   |
| Auth model                     | Token attachment explicit and visible    | Often implicit (cookies / OBO)   |
| Real-time support              | Needs WebSocket/SignalR wiring           | Built-in SignalR                 |
| Latency sensitivity            | Client-side, tolerant                    | Server round-trip per UI event   |
| Offline / resilience story     | Possible (cached WASM)                   | None                             |
| Complexity for demo            | Medium                                   | Low                              |
| Scalability characteristics    | CDN + stateless API                      | Stateful connections             |
| Debuggability                  | Frontend + backend split                 | Easier end-to-end                |
| Azure cost predictability      | Very good                                | Moderate                         |

**Disadvantages**

* **WASM**

  * Subscriptions + auth headers require more plumbing
  * Slightly more moving parts to explain
* **Server**

  * Harder to explain auth + GraphQL boundary cleanly
  * Stateful SignalR connections can be seen as less cloud-native

👉 **Blazor WebAssembly**
It better demonstrates **modern distributed architecture**, explicit auth, and clean separation—key evaluation points in senior reviews.

**Possible combinations**

* WASM frontend + **SignalR only for subscriptions**
* WASM + **local hosting** for UI, Azure only for API

---

## 2. Data Model Choice

**Evaluation criteria:** schema richness, nested resolvers, mutation clarity, demo realism

| Model                     | Strengths                | Weaknesses           | Signal            |
| ------------------------- | ------------------------ | -------------------- | -------------------- |
| Books / Authors / Reviews | Simple, clean nesting    | Less “business-like” | Good baseline        |
| Tasks / Subtasks / Tags   | Recursive relations      | Slightly abstract    | Shows modeling skill |
| Courses / Lessons         | Clear hierarchy          | CRUD less dynamic    | OK                   |
| Orders / Items            | Strong mutation + totals | More logic needed    | Very strong          |
| Blog / Comments           | Real-time comments fit   | Overdone example     | Neutral              |

**Disadvantages**

* **Too simple** → looks trivial
* **Too complex** → risk of incomplete delivery

**Recommended**
👉 **Orders / Line Items**
It shows:

* Nested queries
* Calculated fields
* Real-time updates (order status)

**Possible combination**

* Orders + Comments (real-time on comments only)

---

## 3. Auth Path: Static Token vs JWT

**Evaluation criteria:** security realism, clarity, delivery risk, documentation quality

| Criteria                  | Static Token | Mock JWT (AAD-like) |  JWT (AAD)  |
| ------------------------- | ------------ | ------------------- | ------------- |
| Realism                   | Low          | Medium              | High                |
| Implementation time       | Very fast    | Medium              | Long                |
| Security realism          | Low          | High                | Very high          |
| Azure alignment           | Weak         | Strong              | Very strong        |
| Docs complexity           | Simple       | Moderate            | Complex            |
| Risk of misconfig         | Very low     | Medium              | High                |
| Demonstrates claims/roles | No           | Yes                 | Yes                 |

**Disadvantages**

* **Static token**

  * Looks toy-like if undocumented
* **Mock JWT**

  * More code paths to explain
  * Validation errors can distract
* **AAD JWT**

  * High risk of misconfiguration
  * Time-consuming to set up properly

👉 **Mock JWT validation (locally mocked issuer)**

**Combination**

* Combine AAD JWT(Production) and Mock JWT(Development). Only turn on Mock JWT for local environments; Claim/role model like AAD(roles, scp, oid)

---

## 4. Real-Time Mechanism

**Evaluation criteria:** reliability, Azure compatibility, conceptual clarity

| Option                            | Pros             | Cons                             |
| --------------------------------- | ---------------- | -------------------------------- |
| GraphQL Subscriptions (WebSocket) | Pure GraphQL     | Azure Functions hosting friction |
| Azure SignalR Service             | Robust, scalable | Extra Azure resource             |
| SignalR (self-hosted)             | Simple, flexible | Scaling concerns                 |
| Polling fallback                  | Easy             | Not real-time                    |

**Disadvantages**

* Subscriptions on Functions are non-trivial
* SignalR adds infra complexity

**Recommended**
👉 **SignalR push triggered from GraphQL mutations**

**Combination**

* GraphQL CRUD
* SignalR channel for updates
* Document how this replaces subscriptions

---

## 5. Infrastructure: Azure vs Local

**Evaluation criteria:** operational maturity, reproducibility, effort vs reward

| Option         | Pros           | Cons                |
| -------------- | -------------- | ------------------- |
| Local only     | Fast, low risk | No cloud signal     |
| Azure optional | Balanced       | Slight doc overhead |
| Azure required | Strong signal  | Time + config risk  |

👉 **Azure optional, local fully supported**

---

## 6. IaC Choice

**Evaluation criteria:** team alignment, readability, redeploy story

| Tool      | Strengths              | Weaknesses          |
| --------- | ---------------------- | ------------------- |
| Bicep     | Azure-native, readable | Azure-only          |
| Terraform | Cross-cloud            | Verbose             |
| azd       | One-command DX         | Less explicit infra |

**Recommended**
👉 **Bicep**

**Combination**

* Bicep + simple `deploy.ps1`
* Optional azd wrapper

## 7. Typed GraphQL Client (Blazor)

**Evaluation criteria:** type safety, DX, build-time validation, subscription support

| Client          | Pros                                 | Cons                    |
| --------------- | ------------------------------------ | ----------------------- |
| StrawberryShake | Strong typing, code-gen, schema sync | Setup complexity        |
| GraphQL.Client  | Lightweight, flexible                | Runtime errors possible |

**Recommended**
👉 **StrawberryShake**

---

## 8. Client Identity Flow (WASM)

| Option     | Pros                 | Cons           |
| ---------- | -------------------- | -------------- |
| MSAL + AAD | Real enterprise flow | Heavy          |
| Mock token | Simple               | Less realistic |

**Recommended**
👉 **Mock token**, with MSAL noted as future step

---

## 9. Testing Tools

| Tool               | Impression       |
| ------------------ | ------------------------ |
| Banana Cake Pop    | Modern, GraphQL-native ⭐ |
| GraphQL Playground | Familiar                 |
| Postman            | Enterprise-friendly      |

**Recommended**
👉 **Banana Cake Pop + Postman example**

---

## 10. Concurrent Query Demo

**Good scenarios**

* Load Orders + Items + Customer in parallel
* Batch status counts + detail list

**Should include**

* Explicit `Task.WhenAll`
* Clear explanation why it matters

---

## 11. Error Handling Scenarios

| Error                   | Why it matters      |
| ----------------------- | ------------------- |
| Validation error        | Client UX realism   |
| AuthZ forbidden         | Security clarity    |
| Conflict / stale update | Real-world thinking |

**Recommended**
👉 Validation + Permission denied

---


## 12. Quality Attributes

### 12.1 Error Handling
- Structured GraphQL error extensions with codes (UNAUTHENTICATED, FORBIDDEN, etc.)
- Partial response support for graceful degradation
- Document token acquisition & validation


### 12.2 Testing Strategy
- Unit tests:  Resolvers + Services with xUnit/Moq
- Integration tests: Full GraphQL flow with WebApplicationFactory
- Priority: Auth scenarios + nested query correctness

### 12.3 Observability
- Structured logging (JSON console for containers)
- Application Insights for Azure deployment
- Correlation IDs via Activity.Current.Id

### 12.4 GraphQL Schema & Custom Resolvers

- Include complete schema.graphql
- At least one custom resolver (calculated field, auth-guarded field)

---

## Final Recommended Stack

| Layer     | Choice                  |
| --------- | ----------------------- |
| UI        | Blazor WASM             |
| API       | Azure Functions GraphQL |
| Model     | Orders + Items          |
| Auth      | Mock JWT                |
| Real-time | SignalR push            |
| IaC       | Bicep                   |
| Hosting   | Local + optional Azure  |
| Testing   | Banana Cake Pop         |
