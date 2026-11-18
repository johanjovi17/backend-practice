using BackendPractice.Data;
using BackendPractice.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendPractice.Services;

public sealed class EnrollmentWorker(
    IServiceScopeFactory scopeFactory,
    EnrollmentMessageQueue messageQueue,
    ILogger<EnrollmentWorker> logger) : BackgroundService
{
    private const int MaximumRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in messageQueue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessMessageAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                await HandleFailureAsync(message, exception, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(
        EnrollmentCreatedMessage message,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseDbContext>();

        var processedMessage = await dbContext.ProcessedMessages.FindAsync(
            [message.MessageId],
            cancellationToken);
        if (processedMessage?.Status == ProcessedMessageStatus.Completed)
        {
            return;
        }

        var enrollment = await dbContext.Enrollments.FindAsync(
            [message.Payload.EnrollmentId],
            cancellationToken);
        var student = await dbContext.Users.FindAsync([message.Payload.StudentId], cancellationToken);
        var course = await dbContext.Courses.FindAsync([message.Payload.CourseId], cancellationToken);
        if (enrollment is null || student is null || course is null)
        {
            throw new InvalidOperationException($"Enrollment message {message.MessageId} refers to missing data.");
        }

        if (processedMessage is null)
        {
            dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                MessageId = message.MessageId,
                ProcessedAt = DateTime.UtcNow,
                Status = ProcessedMessageStatus.Processing
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Sending enrollment confirmation for enrollment {EnrollmentId} to student {StudentId} for course {CourseId}.",
            enrollment.Id,
            student.Id,
            course.Id);
        logger.LogInformation(
            "Updating enrollment analytics for course {CourseId}.",
            course.Id);

        processedMessage ??= await dbContext.ProcessedMessages.FindAsync(
            [message.MessageId],
            cancellationToken);
        processedMessage!.Status = ProcessedMessageStatus.Completed;
        processedMessage.ProcessedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleFailureAsync(
        EnrollmentCreatedMessage message,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Could not process enrollment message {MessageId}.", message.MessageId);

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
        var outboxMessage = await dbContext.OutboxMessages.FindAsync([message.MessageId], cancellationToken);
        if (outboxMessage is null)
        {
            logger.LogError("Outbox message {MessageId} was not found for retry.", message.MessageId);
            return;
        }

        if (outboxMessage.RetryCount < MaximumRetries)
        {
            outboxMessage.RetryCount++;
            await dbContext.SaveChangesAsync(cancellationToken);
            await messageQueue.EnqueueAsync(
                message with { RetryCount = outboxMessage.RetryCount },
                cancellationToken);
            return;
        }

        dbContext.DeadLetterMessages.Add(new DeadLetterMessage
        {
            MessageId = message.MessageId,
            EventType = message.EventType,
            Payload = outboxMessage.Payload,
            FailedAt = DateTime.UtcNow,
            Error = exception.Message
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogError("Enrollment message {MessageId} moved to the dead-letter queue.", message.MessageId);
    }
}
