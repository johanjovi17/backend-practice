using System.ComponentModel.DataAnnotations;

namespace BackendPractice.Models;

public sealed class CourseRequest
{
    [Required]
    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? InstructorName { get; init; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? Price { get; init; }

    [Required]
    public string? Category { get; init; }

    [Required]
    public string? Status { get; init; }
}
