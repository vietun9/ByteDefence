# BookStore GraphQL + Blazor WebAssembly

A full-stack application demonstrating Azure Functions GraphQL API with a Blazor WebAssembly frontend.

## Architecture Overview

### Technology Stack

| Component | Technology |
|-----------|------------|
| **Backend** | Azure Functions v4 (.NET 8 Isolated Worker) |
| **GraphQL** | HotChocolate 15.x |
| **Database** | EF Core with In-Memory Provider |
| **Frontend** | Blazor WebAssembly (.NET 8) |
| **GraphQL Client** | GraphQL.Client |
| **Authentication** | Bearer Token (JWT / Static Demo Token) |

### Why Blazor WebAssembly?

I chose **Blazor WebAssembly** over Blazor Server for this project because:

1. **Azure Static Web Apps Ready**: WASM can be deployed as static files to Azure Static Web Apps
2. **Offline Capability**: Client-side execution allows for potential offline support
3. **Reduced Server Load**: Processing happens on the client, reducing API server load
4. **Decoupled Architecture**: Clean separation between frontend and backend
5. **API-First**: Works well with GraphQL API approach

### Project Structure

```
src/
├── BookStore.Api/                  # Azure Functions GraphQL API
│   ├── Auth/                       # JWT authentication services
│   ├── Data/                       # EF Core DbContext and seeding
│   ├── GraphQL/                    # GraphQL types, queries, mutations, subscriptions
│   ├── Models/                     # Domain models
│   ├── GraphQLFunction.cs          # GraphQL HTTP endpoint
│   ├── TokenFunction.cs            # Token generation endpoint
│   ├── schema.graphql              # Complete GraphQL schema
│   └── Program.cs                  # Service configuration
│
└── BookStore.BlazorClient/         # Blazor WASM Frontend
    ├── Models/                     # Client-side models
    ├── Services/                   # GraphQL service and auth handlers
    ├── Pages/                      # Razor pages (List, Detail, Create, Edit)
    └── wwwroot/                    # Static assets and configuration
```

## Quick Start (Local Development)

### Prerequisites

- .NET 8.0 SDK
- Azure Functions Core Tools v4 (optional, for Functions runtime)

### Running the API

```bash
# Navigate to API project
cd src/BookStore.Api

# Run the Azure Functions host
func start
# OR using dotnet
dotnet run
```

The API will be available at `http://localhost:7071`

### Running the Blazor Client

```bash
# In a new terminal, navigate to client project
cd src/BookStore.BlazorClient

# Run the Blazor app
dotnet run
```

The client will be available at `http://localhost:5000` or `https://localhost:5001`

### Configuration

The Blazor client reads API URL from `wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "http://localhost:7071/api"
}
```

For production, create `appsettings.Production.json` with the deployed API URL.

## Authentication

### Bearer Token Options

The API supports two authentication methods:

#### 1. Static Demo Token (Recommended for Testing)

Use this token in the `Authorization` header:

```
Authorization: Bearer demo-bearer-token-2024
```

#### 2. JWT Token

Generate a JWT token using the token endpoint:

```bash
# Generate a JWT token
curl -X POST http://localhost:7071/api/token \
  -H "Content-Type: application/json" \
  -d '{"userId": "user-1", "userName": "Test User", "role": "User"}'
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1...",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

### Auth Info Endpoint

Get authentication information:

```bash
curl http://localhost:7071/api/auth-info
```

### Protected Operations

The following GraphQL mutations require authentication:
- `createBook`
- `updateBook`
- `deleteBook`
- `createAuthor`
- `createReview`

Queries are public and do not require authentication.

## GraphQL API

### Endpoint

```
POST http://localhost:7071/api/graphql
```

### GraphQL Playground / Banana Cake Pop

Access the GraphQL IDE at:
```
http://localhost:7071/api/graphql
```

### Sample Queries

#### Get All Books

```graphql
query GetBooks {
  books {
    id
    title
    description
    publishedYear
    status
    author {
      id
      name
    }
    averageRating
    reviewCount
  }
}
```

#### Get Book by ID

```graphql
query GetBookById($id: String!) {
  bookById(id: $id) {
    id
    title
    description
    isbn
    publishedYear
    status
    author {
      id
      name
      biography
    }
    reviews {
      id
      title
      content
      rating
      reviewerName
      createdAt
    }
    averageRating
    reviewCount
  }
}
```

Variables:
```json
{
  "id": "book-1"
}
```

#### Concurrent Data Query

Demonstrates parallel database queries:

```graphql
query ConcurrentData {
  concurrentData {
    books {
      id
      title
    }
    authors {
      id
      name
    }
    reviews {
      id
      title
    }
  }
}
```

#### Error Handling Example

```graphql
query TestError {
  bookWithError(simulateError: true) {
    id
    title
  }
}
```

### Sample Mutations

#### Create Book (Requires Auth)

```graphql
mutation CreateBook($input: CreateBookInput!) {
  createBook(input: $input) {
    book {
      id
      title
      author {
        name
      }
    }
    errors {
      message
      code
    }
  }
}
```

Variables:
```json
{
  "input": {
    "title": "New Book",
    "description": "A great new book",
    "isbn": "978-1234567890",
    "publishedYear": 2024,
    "authorId": "author-1",
    "status": "DRAFT"
  }
}
```

Headers:
```
Authorization: Bearer demo-bearer-token-2024
```

#### Update Book (Requires Auth)

```graphql
mutation UpdateBook($input: UpdateBookInput!) {
  updateBook(input: $input) {
    book {
      id
      title
      status
    }
    errors {
      message
      code
    }
  }
}
```

Variables:
```json
{
  "input": {
    "id": "book-1",
    "title": "Updated Title",
    "status": "PUBLISHED"
  }
}
```

#### Delete Book (Requires Auth)

```graphql
mutation DeleteBook($id: String!) {
  deleteBook(id: $id) {
    success
    errors {
      message
      code
    }
  }
}
```

#### Create Review (Requires Auth)

```graphql
mutation CreateReview($input: CreateReviewInput!) {
  createReview(input: $input) {
    review {
      id
      title
      rating
    }
    errors {
      message
      code
    }
  }
}
```

Variables:
```json
{
  "input": {
    "bookId": "book-1",
    "title": "Great Read!",
    "content": "Highly recommend this book.",
    "rating": 5,
    "reviewerName": "BookFan"
  }
}
```

### Subscriptions (Real-time)

#### Subscribe to Book Created

```graphql
subscription OnBookCreated {
  onBookCreated {
    id
    title
    author {
      name
    }
  }
}
```

#### Subscribe to Book Updated

```graphql
subscription OnBookUpdated {
  onBookUpdated {
    id
    title
    status
  }
}
```

#### Subscribe to Review Added

```graphql
subscription OnReviewAdded {
  onReviewAdded {
    id
    title
    rating
    bookId
  }
}
```

## Testing with Postman

1. **Import the GraphQL schema** from `src/BookStore.Api/schema.graphql`

2. **Create a new GraphQL request** with URL: `http://localhost:7071/api/graphql`

3. **For mutations**, add the Authorization header:
   ```
   Authorization: Bearer demo-bearer-token-2024
   ```

4. **Run queries and mutations** using the examples above

## Frontend Features

The Blazor WebAssembly frontend includes:

- **Books List Page** (`/books`): View all books with filtering and sorting
- **Book Detail Page** (`/books/{id}`): View book details, author info, and reviews
- **Create Book Page** (`/books/create`): Add new books with author selection
- **Edit Book Page** (`/books/edit/{id}`): Update existing book information
- **Authors Page** (`/authors`): Browse all authors

### UI Features

- Loading states for all data operations
- Error handling with user-friendly messages
- Environment-based API endpoint configuration
- Automatic bearer token attachment for API calls

## Custom Resolvers

The API includes custom resolvers demonstrating GraphQL field resolution:

1. **`averageRating`** on Book type - Calculates average review rating
2. **`reviewCount`** on Book type - Returns total review count
3. **`bookCount`** on Author type - Returns author's book count

## Data Model

### Book

```
Book {
  id: String!
  title: String!
  description: String
  isbn: String
  publishedYear: Int!
  status: BookStatus!
  createdAt: DateTime!
  updatedAt: DateTime!
  authorId: String!
  author: Author
  reviews: [Review!]!
  averageRating: Float
  reviewCount: Int!
}
```

### Author

```
Author {
  id: String!
  name: String!
  biography: String
  createdAt: DateTime!
  updatedAt: DateTime!
  books: [Book!]!
  bookCount: Int!
}
```

### Review

```
Review {
  id: String!
  title: String!
  content: String
  rating: Int!
  reviewerName: String!
  createdAt: DateTime!
  updatedAt: DateTime!
  bookId: String!
  book: Book
}
```

### BookStatus Enum

```
enum BookStatus {
  DRAFT
  PUBLISHED
  OUT_OF_PRINT
  ARCHIVED
}
```

## Seed Data

The application comes with pre-seeded data:

### Authors
- George Orwell
- Jane Austen
- Isaac Asimov

### Books
- 1984 (George Orwell)
- Animal Farm (George Orwell)
- Pride and Prejudice (Jane Austen)
- Foundation (Isaac Asimov)

### Reviews
- Sample reviews for each book

## AI Usage Summary

This project was developed with AI assistance using the following approach:

1. **Architecture Planning**: Used AI to evaluate technology choices (Azure Functions vs Container Apps, Blazor WASM vs Server)
2. **GraphQL Schema Design**: AI helped design the complete schema with proper types, inputs, and payloads
3. **HotChocolate Configuration**: AI assisted with HotChocolate setup for Azure Functions
4. **Authentication Implementation**: AI helped implement JWT validation and authentication interceptor
5. **Blazor Client Development**: AI generated the GraphQL service wrapper and Razor pages
6. **Documentation**: AI assisted in creating comprehensive documentation

Key prompts used:
- "Create an Azure Functions project with HotChocolate GraphQL"
- "Implement JWT authentication for GraphQL mutations"
- "Create Blazor WASM pages for CRUD operations"
- "Add custom resolvers for computed fields"

## License

This project is provided as-is for educational purposes.
