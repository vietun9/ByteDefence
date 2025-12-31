## Problem Understanding
---

## 1. Core problem to solve

- Build a modern, cloud-ready, scalable backend–frontend system
- Simulate **internal business system**:
    - Main entity (Orders / Tasks / Courses…)
    - Nested data (Order → Items, Task → Subtasks)
    - Concurrent access
    - Real-time update
    - Security boundary
    - Deployable & operable

---

## 2. What is NOT being evaluated?

- Full production system
- Beauty UI
- Complex business logic
- High performance optimization
- Full production-grade security
- Realistic domain complexity

👉 **Mock data, mock auth, mock error is acceptable.**

---

## 3. Key mindset & skills being evaluated

### 3.1. Architectural thinking

- **NOT LIMITED TO TECHNICAL STACK CHOICES**
- What layers do you separate the system into?
- Why Blazor WASM or Server?
- Where do you place auth (middleware, directive, resolver)?
- How do you handle real-time when GraphQL subscriptions are inconvenient?

💡 It doesn't need to be "absolutely correct", but **must have a clear reason**.

---

### 3.2. True understanding of GraphQL

- Clear schema design, typed
- Query / Mutation / Subscription (or equivalent)
- Resolver with single responsibility
- Nested resolving (not flat REST-style DTOs)

---

### 3.3. Working with concurrency & real-time

- Concurrent database queries
- Real-time update (subscriptions or SignalR)

---

### 3.4. Security awareness (not full security)

No need for:
* OAuth flow full implementation
* Refresh token
* Key Vault

But must show of:

* JWT 
* Bearer token
* 401 vs 403 responses
* AuthZ rule enforcement
* Client attach token correctly

---

### 3.5. Deployment & Infrastructure awareness

- If local only, how to run end-to-end with Container/Functions
- If deploy Azure so how?
- Where infra lives
- How config per environment
- Redeploy repeatable or not
- How to test after deploy

---

## 4. Expected minimum skills demonstrated

#### Backend

- Design GraphQL schema well
- Implement resolvers, directives, middleware
- Simulate real-time
- Handle error intentionally

#### Frontend

- Don't hardcode
- Handle loading / error state
- Consume GraphQL correctly
- Understand auth client-side

#### Documentation

- To nomarl developer can follow
- Make some require testing steps clear
- Know limitations and trade-offs made