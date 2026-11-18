namespace BackendPractice.Models;

public sealed class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrolledAt { get; set; }
    public EnrollmentStatus Status { get; set; }
}

public enum EnrollmentStatus
{
    Active
}
