namespace BackendPractice.Models;

public sealed record EnrollmentCreatedPayload(int EnrollmentId, int StudentId, int CourseId);

public sealed record EnrollmentCreatedMessage(
    Guid MessageId,
    string EventType,
    EnrollmentCreatedPayload Payload,
    int RetryCount = 0);
