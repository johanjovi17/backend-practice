using BackendPractice.Models;

namespace BackendPractice.Services;

public interface IEnrollmentService
{
    Task<EnrollmentResult> CreateAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken);
}
