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

    public async Task<CourseQueryResult> GetCoursesAsync(
        CourseQueryParameters parameters,
        bool includeUnpublished,
        CancellationToken cancellationToken)
    {
        var page = parameters.Page <= 0 ? 1 : parameters.Page;
        var pageSize = parameters.PageSize <= 0 ? 10 : Math.Min(parameters.PageSize, 50);
        var sortBy = parameters.SortBy ?? "createdAt";
        var sortOrder = parameters.SortOrder ?? "asc";
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (parameters.MinPrice < 0)
        {
            errors["minPrice"] = ["Minimum price cannot be negative."];
        }

        if (parameters.MaxPrice < 0)
        {
            errors["maxPrice"] = ["Maximum price cannot be negative."];
        }

        if (parameters.MinPrice.HasValue &&
            parameters.MaxPrice.HasValue &&
            parameters.MinPrice > parameters.MaxPrice)
        {
            errors["minPrice"] = ["Minimum price cannot be greater than maximum price."];
        }

        if (!new[] { "title", "price", "createdAt" }.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
        {
            errors["sortBy"] = ["Sort by must be title, price, or createdAt."];
        }

        if (!new[] { "asc", "desc" }.Contains(sortOrder, StringComparer.OrdinalIgnoreCase))
        {
            errors["sortOrder"] = ["Sort order must be asc or desc."];
        }

        CourseStatus? status = null;
        if (parameters.Status is not null)
        {
            var statusName = Enum.GetNames<CourseStatus>().FirstOrDefault(
                name => string.Equals(name, parameters.Status, StringComparison.OrdinalIgnoreCase));
            if (statusName is null)
            {
                errors["status"] = ["Status must be Draft, Published, or Archived."];
            }
            else
            {
                status = Enum.Parse<CourseStatus>(statusName);
            }
        }

        if (errors.Count > 0)
        {
            return CourseQueryResult.Invalid(errors);
        }

        IQueryable<Course> query = dbContext.Courses.AsNoTracking();
        if (!includeUnpublished)
        {
            query = query.Where(course => course.Status == CourseStatus.Published);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Category))
        {
            query = query.Where(course => course.Category == parameters.Category);
        }

        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(course => course.Price >= parameters.MinPrice.Value);
        }

        if (parameters.MaxPrice.HasValue)
        {
            query = query.Where(course => course.Price <= parameters.MaxPrice.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(course => course.Status == status.Value);
        }

        var totalRecords = await query.CountAsync(cancellationToken);
        var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        var orderedQuery = sortBy.ToLowerInvariant() switch
        {
            "title" => descending
                ? query.OrderByDescending(course => course.Title).ThenBy(course => course.Id)
                : query.OrderBy(course => course.Title).ThenBy(course => course.Id),
            "price" => descending
                ? query.OrderByDescending(course => course.Price).ThenBy(course => course.Id)
                : query.OrderBy(course => course.Price).ThenBy(course => course.Id),
            _ => descending
                ? query.OrderByDescending(course => course.CreatedAt).ThenBy(course => course.Id)
                : query.OrderBy(course => course.CreatedAt).ThenBy(course => course.Id)
        };

        var skip = (long)(page - 1) * pageSize;
        var courses = skip > int.MaxValue
            ? new List<Course>()
            : await orderedQuery.Skip((int)skip).Take(pageSize).ToListAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        return CourseQueryResult.Success(new CoursePage(totalPages, courses, totalRecords));
    }

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
