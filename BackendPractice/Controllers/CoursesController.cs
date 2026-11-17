using BackendPractice.Models;
using BackendPractice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendPractice.Controllers;

[ApiController]
[Route("api/courses")]
public sealed class CoursesController(ICourseService courseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Course>>> GetAll(CancellationToken cancellationToken)
    {
        var courses = await courseService.GetAllAsync(cancellationToken);
        return Ok(User.IsInRole(nameof(UserRole.Admin))
            ? courses
            : courses.Where(course => course.Status == CourseStatus.Published));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Course>> GetById(int id, CancellationToken cancellationToken)
    {
        var course = await courseService.GetByIdAsync(id, cancellationToken);
        if (course is not null &&
            course.Status != CourseStatus.Published &&
            !User.IsInRole(nameof(UserRole.Admin)))
        {
            return NotFound();
        }

        return course is null ? NotFound() : Ok(course);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<Course>> Create(
        CourseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await courseService.CreateAsync(request, cancellationToken);
        if (result.Errors is not null)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors));
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Course!.Id }, result.Course);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<Course>> Update(
        int id,
        CourseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await courseService.UpdateAsync(id, request, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        if (result.Errors is not null)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors));
        }

        return Ok(result.Course);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await courseService.DeleteAsync(id, cancellationToken);
        return deleted
            ? Ok(new { message = "Course deleted successfully." })
            : NotFound();
    }
}
