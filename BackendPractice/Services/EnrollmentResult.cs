using BackendPractice.Models;

namespace BackendPractice.Services;

public sealed record EnrollmentResult(
    Enrollment? Enrollment,
    Dictionary<string, string[]>? Errors,
    bool AlreadyEnrolled)
{
    public static EnrollmentResult Success(Enrollment enrollment) => new(enrollment, null, false);

    public static EnrollmentResult Invalid(Dictionary<string, string[]> errors) => new(null, errors, false);

    public static EnrollmentResult Conflict() => new(null, null, true);
}
