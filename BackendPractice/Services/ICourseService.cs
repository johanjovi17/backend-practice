using BackendPractice.Models;

namespace BackendPractice.Services;

public interface ICourseService
{
    Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken);
    Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<CourseMutationResult> CreateAsync(CourseRequest request, CancellationToken cancellationToken);
    Task<CourseMutationResult?> UpdateAsync(int id, CourseRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
