namespace BookStore.BlazorClient.Models;

public record CreateBookInput(
    string Title,
    string? Description,
    string? Isbn,
    int PublishedYear,
    string AuthorId,
    BookStatus? Status);

public record UpdateBookInput(
    string Id,
    string? Title,
    string? Description,
    string? Isbn,
    int? PublishedYear,
    string? AuthorId,
    BookStatus? Status);

public record CreateAuthorInput(
    string Name,
    string? Biography);

public record CreateReviewInput(
    string BookId,
    string Title,
    string? Content,
    int Rating,
    string ReviewerName);

public class UserError
{
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class BookPayload
{
    public Book? Book { get; set; }
    public List<UserError> Errors { get; set; } = new();
}

public class AuthorPayload
{
    public Author? Author { get; set; }
    public List<UserError> Errors { get; set; } = new();
}

public class ReviewPayload
{
    public Review? Review { get; set; }
    public List<UserError> Errors { get; set; } = new();
}

public class DeletePayload
{
    public bool Success { get; set; }
    public List<UserError> Errors { get; set; } = new();
}
