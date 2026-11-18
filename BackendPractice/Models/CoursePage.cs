namespace BackendPractice.Models;

public sealed record CoursePage(
    int TotalPages,
    IReadOnlyList<Course> Courses,
    int TotalRecords);
