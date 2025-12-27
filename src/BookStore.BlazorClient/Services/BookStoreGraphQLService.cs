using BookStore.BlazorClient.Models;
using GraphQL;
using GraphQL.Client.Abstractions;
using System.Reactive.Linq;

namespace BookStore.BlazorClient.Services;

public class BookStoreGraphQLService
{
    private readonly IGraphQLClient _client;

    public BookStoreGraphQLService(IGraphQLClient client)
    {
        _client = client;
    }

    // Query: Get all books
    public async Task<List<Book>> GetBooksAsync()
    {
        var query = new GraphQLRequest
        {
            Query = @"
                query GetBooks {
                    books {
                        id
                        title
                        description
                        isbn
                        publishedYear
                        status
                        createdAt
                        updatedAt
                        authorId
                        author {
                            id
                            name
                        }
                        averageRating
                        reviewCount
                    }
                }"
        };

        var response = await _client.SendQueryAsync<BooksResponse>(query);
        if (response.Errors?.Any() == true)
        {
            throw new GraphQLException(string.Join(", ", response.Errors.Select(e => e.Message)));
        }
        return response.Data?.Books ?? new List<Book>();
    }

    // Query: Get book by ID
    public async Task<Book?> GetBookByIdAsync(string id)
    {
        var query = new GraphQLRequest
        {
            Query = @"
                query GetBookById($id: String!) {
                    bookById(id: $id) {
                        id
                        title
                        description
                        isbn
                        publishedYear
                        status
                        createdAt
                        updatedAt
                        authorId
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
                }",
            Variables = new { id }
        };

        var response = await _client.SendQueryAsync<BookByIdResponse>(query);
        if (response.Errors?.Any() == true)
        {
            throw new GraphQLException(string.Join(", ", response.Errors.Select(e => e.Message)));
        }
        return response.Data?.BookById;
    }

    // Query: Get all authors
    public async Task<List<Author>> GetAuthorsAsync()
    {
        var query = new GraphQLRequest
        {
            Query = @"
                query GetAuthors {
                    authors {
                        id
                        name
                        biography
                        bookCount
                    }
                }"
        };

        var response = await _client.SendQueryAsync<AuthorsResponse>(query);
        if (response.Errors?.Any() == true)
        {
            throw new GraphQLException(string.Join(", ", response.Errors.Select(e => e.Message)));
        }
        return response.Data?.Authors ?? new List<Author>();
    }

    // Mutation: Create book
    public async Task<BookPayload> CreateBookAsync(CreateBookInput input)
    {
        var mutation = new GraphQLRequest
        {
            Query = @"
                mutation CreateBook($input: CreateBookInput!) {
                    createBook(input: $input) {
                        book {
                            id
                            title
                            description
                            isbn
                            publishedYear
                            status
                            authorId
                            author {
                                id
                                name
                            }
                        }
                        errors {
                            message
                            code
                        }
                    }
                }",
            Variables = new { input }
        };

        var response = await _client.SendMutationAsync<CreateBookResponse>(mutation);
        if (response.Errors?.Any() == true)
        {
            throw new GraphQLException(string.Join(", ", response.Errors.Select(e => e.Message)));
        }
        return response.Data?.CreateBook ?? new BookPayload();
    }

    // Mutation: Update book
    public async Task<BookPayload> UpdateBookAsync(UpdateBookInput input)
    {
        var mutation = new GraphQLRequest
        {
            Query = @"
                mutation UpdateBook($input: UpdateBookInput!) {
                    updateBook(input: $input) {
                        book {
                            id
                            title
                            description
                            isbn
                            publishedYear
                            status
                            authorId
                            author {
                                id
                                name
                            }
                        }
                        errors {
                            message
                            code
                        }
                    }
                }",
            Variables = new { input }
        };

        var response = await _client.SendMutationAsync<UpdateBookResponse>(mutation);
        if (response.Errors?.Any() == true)
        {
            throw new GraphQLException(string.Join(", ", response.Errors.Select(e => e.Message)));
        }
        return response.Data?.UpdateBook ?? new BookPayload();
    }

    // Mutation: Delete book
    public async Task<DeletePayload> DeleteBookAsync(string id)
    {
        var mutation = new GraphQLRequest
        {
            Query = @"
                mutation DeleteBook($id: String!) {
                    deleteBook(id: $id) {
                        success
                        errors {
                            message
                            code
                        }
                    }
                }",
            Variables = new { id }
        };

        var response = await _client.SendMutationAsync<DeleteBookResponse>(mutation);
        if (response.Errors?.Any() == true)
        {
            throw new GraphQLException(string.Join(", ", response.Errors.Select(e => e.Message)));
        }
        return response.Data?.DeleteBook ?? new DeletePayload();
    }

    // Mutation: Create author
    public async Task<AuthorPayload> CreateAuthorAsync(CreateAuthorInput input)
    {
        var mutation = new GraphQLRequest
        {
            Query = @"
                mutation CreateAuthor($input: CreateAuthorInput!) {
                    createAuthor(input: $input) {
                        author {
                            id
                            name
                            biography
                        }
                        errors {
                            message
                            code
                        }
                    }
                }",
            Variables = new { input }
        };

        var response = await _client.SendMutationAsync<CreateAuthorResponse>(mutation);
        if (response.Errors?.Any() == true)
        {
            throw new GraphQLException(string.Join(", ", response.Errors.Select(e => e.Message)));
        }
        return response.Data?.CreateAuthor ?? new AuthorPayload();
    }

    // Mutation: Create review
    public async Task<ReviewPayload> CreateReviewAsync(CreateReviewInput input)
    {
        var mutation = new GraphQLRequest
        {
            Query = @"
                mutation CreateReview($input: CreateReviewInput!) {
                    createReview(input: $input) {
                        review {
                            id
                            title
                            content
                            rating
                            reviewerName
                            bookId
                        }
                        errors {
                            message
                            code
                        }
                    }
                }",
            Variables = new { input }
        };

        var response = await _client.SendMutationAsync<CreateReviewResponse>(mutation);
        if (response.Errors?.Any() == true)
        {
            throw new GraphQLException(string.Join(", ", response.Errors.Select(e => e.Message)));
        }
        return response.Data?.CreateReview ?? new ReviewPayload();
    }

    // Response classes
    private class BooksResponse { public List<Book>? Books { get; set; } }
    private class BookByIdResponse { public Book? BookById { get; set; } }
    private class AuthorsResponse { public List<Author>? Authors { get; set; } }
    private class CreateBookResponse { public BookPayload? CreateBook { get; set; } }
    private class UpdateBookResponse { public BookPayload? UpdateBook { get; set; } }
    private class DeleteBookResponse { public DeletePayload? DeleteBook { get; set; } }
    private class CreateAuthorResponse { public AuthorPayload? CreateAuthor { get; set; } }
    private class CreateReviewResponse { public ReviewPayload? CreateReview { get; set; } }
}

public class GraphQLException : Exception
{
    public GraphQLException(string message) : base(message) { }
}
