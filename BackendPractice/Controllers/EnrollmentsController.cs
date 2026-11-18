using System.Globalization;
using BackendPractice.Models;
using BackendPractice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendPractice.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize]
public sealed class EnrollmentsController(IEnrollmentService enrollmentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(
                User.FindFirst("sub")?.Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var studentId))
        {
            return Unauthorized();
        }

        var result = await enrollmentService.CreateAsync(studentId, request.CourseId, cancellationToken);
        if (result.Errors is not null)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors));
        }

        if (result.AlreadyEnrolled)
        {
            return Conflict(new ProblemDetails { Title = "You are already enrolled in this course." });
        }

        return StatusCode(StatusCodes.Status201Created, result.Enrollment);
    }
}
