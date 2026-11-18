using System.ComponentModel.DataAnnotations;

namespace BackendPractice.Models;

public sealed class CreateEnrollmentRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; init; }
}
