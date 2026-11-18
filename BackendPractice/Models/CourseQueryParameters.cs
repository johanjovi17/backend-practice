namespace BackendPractice.Models;

public sealed class CourseQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Category { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public string? SortOrder { get; set; } = "asc";
    public string? Status { get; set; }
}
