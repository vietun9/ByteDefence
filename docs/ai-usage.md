# AI Usage Summary
Step-by-step guide for using AI in this project

## 1. Get Base Project Requirements from PDF

## 2. Let AI Create Personal Understanding of Project/Requirements
**Prompt:**
```
Based on the requirements from @[project-base-requirements.pdf], create a 
comprehensive personal understanding document that includes:
- High-level project overview
- Key features and functionalities
- Rules and constraints
- Success criteria
Save this as @[docs/personal-understanding.md]
```

### 2.1. Self Validate Personal Understanding
*Manual review: Verify @[docs/personal-understanding.md] against @[project-base-requirements.pdf] to identify gaps or misinterpretations.*

## 3. Let AI Extract Criteria for Architecture Decisions
**Prompt:**
```
Analyze @[docs/personal-understanding.md] and @[docs/project-base-requirements.md] 
to extract all criteria that will influence architecture decisions, focusing on:
- Cloud-native and serverless scalability (Azure)
- Real-time collaboration needs (SignalR)
- Modern .NET ecosystem compatibility (Blazor, GraphQL)
- Security and compliance (Key Vault, Identity)
- DevOps and Infrastructure as Code requirements (Bicep)
Save this as @[docs/architect-decisions.md]
```

### 3.1. Self Validate Criteria
*Manual review: Verify @[docs/architect-decisions.md] captures all critical criteria by cross-referencing with base requirements.*

## 4. Let AI Suggest Architecture and Technology Decisions
**Prompt:**
```
Based on the criteria in @[docs/architect-decisions.md] and @[docs/personal-understanding.md], suggest specific architecture and technology stack decisions. 
Provide detailed reasoning for each decision as table
Save this as @[docs/architect-decisions.md]
```

### 4.1. Self Validate Architecture and Technology Decisions
*Manual review: Evaluate @[docs/architect-decisions.md] to ensure it meets all requirements and uses appropriate technologies.*

### 4.2. Self learn New Technologies
*Self learn technologies never used before*

## 5. Let AI Create Implementation Plan
**Prompt:**
```
Based on @[docs/architect-decisions.md] and @[docs/personal-understanding.md], create a detailed **Implementation Plan** that includes:
- Project scaffolding, Docker, Basic GraphQL
- Core GraphQL (Schema, Resolvers, EF Core)
- Auth (JWT, Middleware, Directives)
- Real-time (SignalR Hub, Notifications)
- Frontend (Blazor pages, Components)
- Testing (Unit, Integration)
- Infrastructure (Bicep, GitHub Actions)
Include detailed tasks, code snippets for critical parts, and exit criteria for each phase.
Save this as @[docs/implement-plan.md]
```

### 5.1. Self Validate Implementation Plan
*Manual review: Verify @[docs/implement-plan.md] includes all features and let AI pair validate it with*

## 6. Let AI Start Implementing Project
**Prompt:**
```
Following @[docs/implement-plan.md], implement all features.
Ensure:
- Code follows the architecture decisions
- Security considerations are addressed
```

### 6.1. Let AI Test Implementation
**Prompt:**
```
Verify all implemented features are working as expected of @[docs/implement-plan.md] and @[docs/personal-understanding.md] and @[project-base-requirements.pdf]
Create and run tests for the current phase:
- Write unit tests for resolvers using xUnit and Moq
- Write integration tests for GraphQL endpoints
- Verify authentication and authorization flows
- Test real-time updates using SignalR clients
Run the tests and fix any failures.
```

### 6.2. Let AI Verify Implementation Against Requirements
**Prompt:**
```
Verify that the complete implementation matches:
- @[project-base-requirements.pdf]
- @[docs/personal-understanding.md]
Create a verification report that:
- Maps each requirement to its implementation in the code
- Identifies any missing or partially implemented requirements
- Documents any deviations from original requirements
- Suggests improvements or fixes needed
Save this as @[docs/verification-report.md]
```

## 7. Self Validate Implementation
*Manual review: Code review of implemented features to verify they match specifications, follow standards, and manual testing core features.*

