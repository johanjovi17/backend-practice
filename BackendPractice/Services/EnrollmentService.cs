using System.Text.Json;
using BackendPractice.Data;
using BackendPractice.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BackendPractice.Services;

public sealed class EnrollmentService(CourseDbContext dbContext) : IEnrollmentService
{
    public async Task<EnrollmentResult> CreateAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == courseId, cancellationToken);
        if (course is null || course.Status != CourseStatus.Published)
        {
            return EnrollmentResult.Invalid(new Dictionary<string, string[]>
            {
                ["courseId"] = ["Course does not exist or is not available."]
            });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (await dbContext.Enrollments.AnyAsync(
                enrollment => enrollment.StudentId == studentId && enrollment.CourseId == courseId,
                cancellationToken))
        {
            return EnrollmentResult.Conflict();
        }

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Active
        };
        dbContext.Enrollments.Add(enrollment);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.OutboxMessages.Add(new OutboxMessage
            {
                EventType = "EnrollmentCreated",
                Payload = JsonSerializer.Serialize(
                    new EnrollmentCreatedPayload(enrollment.Id, studentId, courseId)),
                CreatedAt = DateTime.UtcNow,
                Status = OutboxMessageStatus.Pending
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateEnrollment(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return EnrollmentResult.Conflict();
        }

        return EnrollmentResult.Success(enrollment);
    }

    private static bool IsDuplicateEnrollment(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
