namespace BookStore.BlazorClient.Models;

public class Author
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Book> Books { get; set; } = new();
    public int BookCount { get; set; }
}
