namespace BackendPractice.Models;

public sealed class Course
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? InstructorName { get; set; }
    public int Price { get; set; }
    public required string Category { get; set; }
    public CourseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
