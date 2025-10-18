namespace AIProjectOrchestrator.Domain.Models;

/// <summary>
/// Represents a page of results with metadata for pagination
/// </summary>
/// <typeparam name="T">The type of items in the page</typeparam>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
