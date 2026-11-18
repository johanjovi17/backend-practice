namespace BackendPractice.Models;

public sealed class DeadLetterMessage
{
    public int Id { get; set; }
    public Guid MessageId { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public DateTime FailedAt { get; set; }
    public required string Error { get; set; }
}
