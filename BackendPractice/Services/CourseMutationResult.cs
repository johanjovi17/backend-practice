using BackendPractice.Models;

namespace BackendPractice.Services;

public sealed record CourseMutationResult(Course? Course, Dictionary<string, string[]>? Errors)
{
    public static CourseMutationResult Success(Course course) => new(course, null);

    public static CourseMutationResult Invalid(Dictionary<string, string[]> errors) => new(null, errors);
}
