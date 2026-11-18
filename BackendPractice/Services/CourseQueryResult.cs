using BackendPractice.Models;

namespace BackendPractice.Services;

public sealed record CourseQueryResult(CoursePage? Page, Dictionary<string, string[]>? Errors)
{
    public static CourseQueryResult Success(CoursePage page) => new(page, null);

    public static CourseQueryResult Invalid(Dictionary<string, string[]> errors) => new(null, errors);
}
