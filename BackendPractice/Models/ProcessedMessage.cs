namespace BackendPractice.Models;

public sealed class ProcessedMessage
{
    public Guid MessageId { get; set; }
    public DateTime ProcessedAt { get; set; }
    public ProcessedMessageStatus Status { get; set; }
}

public enum ProcessedMessageStatus
{
    Processing,
    Completed
}
