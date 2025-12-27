namespace BookStore.BlazorClient.Models;

public class Book
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Isbn { get; set; }
    public int PublishedYear { get; set; }
    public BookStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public Author? Author { get; set; }
    public List<Review> Reviews { get; set; } = new();
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public enum BookStatus
{
    DRAFT,
    PUBLISHED,
    OUT_OF_PRINT,
    ARCHIVED
}
