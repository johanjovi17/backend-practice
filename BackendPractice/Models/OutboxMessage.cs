namespace BackendPractice.Models;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public OutboxMessageStatus Status { get; set; }
    public int RetryCount { get; set; }
}

public enum OutboxMessageStatus
{
    Pending,
    Published
}
