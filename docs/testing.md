# Testing Guide

## GraphQL API Testing

### Using Postman

1. Create a new request with method `POST`
2. URL: `http://localhost:7071/api/graphql`
3. Headers:
   - `Content-Type: application/json`
   - `Authorization: Bearer <token>` (for authenticated requests)
4. Body (raw JSON):
   ```json
   {
     "query": "mutation { login(input: { username: \"admin\", password: \"admin123\" }) { token user { id username } } }"
   }
   ```

### Using Banana Cake Pop

1. Download from [Banana Cake Pop](https://chillicream.com/products/bananacakepop)
2. Connect to `http://localhost:7071/api/graphql`
3. Use the built-in explorer to browse the schema
4. Add authentication header after logging in

### Using curl

**Login:**
```bash
curl -X POST http://localhost:7071/api/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "mutation { login(input: { username: \"admin\", password: \"admin123\" }) { token user { id username } errorMessage } }"}'
```

**Get Orders (with token):**
```bash
curl -X POST http://localhost:7071/api/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-token>" \
  -d '{"query": "query { orders { id title status total } }"}'
```

## Test Scenarios

### Authentication Tests

1. **Login with valid credentials**
   ```graphql
   mutation {
     login(input: { username: "admin", password: "admin123" }) {
       token
       user { id username role }
     }
   }
   ```
   Expected: Returns token and user info

2. **Login with invalid credentials**
   ```graphql
   mutation {
     login(input: { username: "admin", password: "wrong" }) {
       token
       errorMessage
     }
   }
   ```
   Expected: Returns error message, no token

3. **Access protected endpoint without token**
   ```graphql
   query { orders { id } }
   ```
   Expected: Returns authentication error

### CRUD Tests

1. **Create Order**
   ```graphql
   mutation {
     createOrder(input: { title: "Test Order", description: "Testing" }) {
       order { id title status }
       errorMessage
     }
   }
   ```

2. **Update Order**
   ```graphql
   mutation {
     updateOrder(input: { id: "order-001", status: APPROVED }) {
       order { id status }
       errorMessage
     }
   }
   ```

3. **Delete Order (Admin only)**
   ```graphql
   mutation {
     deleteOrder(id: "order-001") {
       success
       errorMessage
     }
   }
   ```

4. **Add Item to Order**
   ```graphql
   mutation {
     addOrderItem(input: { 
       orderId: "order-001", 
       name: "New Item", 
       quantity: 5, 
       price: 19.99 
     }) {
       item { id name quantity price subtotal }
       errorMessage
     }
   }
   ```

### Nested Query Tests

1. **Order with Items and Creator**
   ```graphql
   query {
     order(id: "order-001") {
       id
       title
       total
       items {
         name
         quantity
         price
         subtotal
       }
       createdBy {
         username
         email
       }
     }
   }
   ```

### Concurrent Query Test

```graphql
query {
  orderStats {
    totalOrders
    totalUsers
    pendingOrders
    totalValue
  }
}
```
This query demonstrates concurrent database access using Task.WhenAll.

## Real-time Testing

### Test SignalR Connection

1. Open two browser windows with the Blazor app
2. Login to both windows
3. Navigate to Orders page in both
4. Create/update an order in one window
5. Verify the other window shows an update notification

### SignalR Events

- `OrderCreated` - Fired when a new order is created
- `OrderUpdated` - Fired when an order is modified
- `OrderDeleted` - Fired when an order is deleted

## Frontend Testing

### Manual Testing Checklist

- [ ] Login page loads
- [ ] Can login with valid credentials
- [ ] Invalid login shows error message
- [ ] Dashboard shows statistics after login
- [ ] Orders page shows list of orders
- [ ] Can navigate to order details
- [ ] Can create a new order
- [ ] Can edit an existing order
- [ ] Can add items to an order
- [ ] Can remove items from an order
- [ ] Can change order status
- [ ] Real-time updates work (test with two browsers)
- [ ] Connection status indicator works
- [ ] Logout clears session
- [ ] Protected pages redirect to login when not authenticated

## Error Handling Tests

1. **Validation Error**
   ```graphql
   mutation {
     createOrder(input: { title: "", description: "" }) {
       order { id }
       errorMessage
     }
   }
   ```
   Expected: Returns "Title is required"

2. **Not Found Error**
   ```graphql
   query {
     order(id: "non-existent") {
       id
     }
   }
   ```
   Expected: Returns null order

3. **Unauthorized Access**
   - Remove Authorization header
   - Call any protected query
   - Expected: Returns authentication error
