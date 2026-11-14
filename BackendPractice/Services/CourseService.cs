using BackendPractice.Data;
using BackendPractice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BackendPractice.Services;

public sealed class CourseService(
    CourseDbContext dbContext,
    IOptions<CourseOptions> courseOptions) : ICourseService
{
    private readonly HashSet<string> _allowedCategories =
        new(courseOptions.Value.AllowedCategories, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Courses
            .AsNoTracking()
            .OrderBy(course => course.Id)
            .ToListAsync(cancellationToken);

    public Task<Course?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Courses.AsNoTracking().FirstOrDefaultAsync(course => course.Id == id, cancellationToken);

    public async Task<CourseMutationResult> CreateAsync(
        CourseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var status, out var category, out var errors))
        {
            return CourseMutationResult.Invalid(errors);
        }

        var now = DateTime.UtcNow;
        var course = new Course
        {
            Title = request.Title!.Trim(),
            Description = request.Description,
            InstructorName = request.InstructorName,
            Price = request.Price!.Value,
            Category = category,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CourseMutationResult.Success(course);
    }

    public async Task<CourseMutationResult?> UpdateAsync(
        int id,
        CourseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var status, out var category, out var errors))
        {
            return CourseMutationResult.Invalid(errors);
        }

        var course = await dbContext.Courses.FindAsync([id], cancellationToken);
        if (course is null)
        {
            return null;
        }

        course.Title = request.Title!.Trim();
        course.Description = request.Description;
        course.InstructorName = request.InstructorName;
        course.Price = request.Price!.Value;
        course.Category = category;
        course.Status = status;
        course.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return CourseMutationResult.Success(course);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.FindAsync([id], cancellationToken);
        if (course is null)
        {
            return false;
        }

        dbContext.Courses.Remove(course);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private bool TryValidate(
        CourseRequest request,
        out CourseStatus status,
        out string category,
        out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Title is required."];
        }

        if (request.Price is null)
        {
            errors["price"] = ["Price is required."];
        }
        else if (request.Price < 0)
        {
            errors["price"] = ["Price cannot be negative."];
        }

        var requestedCategory = request.Category?.Trim();
        var allowedCategory = _allowedCategories.FirstOrDefault(
            candidate => string.Equals(candidate, requestedCategory, StringComparison.OrdinalIgnoreCase));
        if (allowedCategory is null)
        {
            errors["category"] = [$"Category must be one of: {string.Join(", ", _allowedCategories.Order())}."];
        }

        var statusName = Enum.GetNames<CourseStatus>().FirstOrDefault(
            name => string.Equals(name, request.Status, StringComparison.OrdinalIgnoreCase));
        if (statusName is null)
        {
            errors["status"] = ["Status must be Draft, Published, or Archived."];
            status = default;
        }
        else
        {
            status = Enum.Parse<CourseStatus>(statusName);
        }

        category = allowedCategory ?? string.Empty;
        return errors.Count == 0;
    }
}
