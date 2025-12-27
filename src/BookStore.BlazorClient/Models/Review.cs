namespace BookStore.BlazorClient.Models;

public class Review
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int Rating { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string BookId { get; set; } = string.Empty;
}
