using System.Text.Json;
using BackendPractice.Data;
using BackendPractice.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendPractice.Services;

public sealed class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    EnrollmentMessageQueue messageQueue,
    ILogger<OutboxPublisherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var recoveredPublishedMessages = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!recoveredPublishedMessages)
                {
                    await RepublishUnfinishedMessagesAsync(stoppingToken);
                    recoveredPublishedMessages = true;
                }

                await PublishPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not publish pending enrollment messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task RepublishUnfinishedMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
        var messages = await dbContext.OutboxMessages
            .Where(message =>
                message.Status == OutboxMessageStatus.Published &&
                !dbContext.ProcessedMessages.Any(processed =>
                    processed.MessageId == message.Id &&
                    processed.Status == ProcessedMessageStatus.Completed) &&
                !dbContext.DeadLetterMessages.Any(deadLetter => deadLetter.MessageId == message.Id))
            .OrderBy(message => message.PublishedAt)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            var payload = JsonSerializer.Deserialize<EnrollmentCreatedPayload>(message.Payload)
                ?? throw new JsonException($"Outbox message {message.Id} has an empty payload.");
            await messageQueue.EnqueueAsync(
                new EnrollmentCreatedMessage(message.Id, message.EventType, payload, message.RetryCount),
                cancellationToken);
        }
    }

    private async Task PublishPendingMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
        var messages = await dbContext.OutboxMessages
            .Where(message => message.Status == OutboxMessageStatus.Pending)
            .OrderBy(message => message.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<EnrollmentCreatedPayload>(message.Payload)
                    ?? throw new JsonException($"Outbox message {message.Id} has an empty payload.");
                await messageQueue.EnqueueAsync(
                    new EnrollmentCreatedMessage(message.Id, message.EventType, payload, message.RetryCount),
                    cancellationToken);

                message.Status = OutboxMessageStatus.Published;
                message.PublishedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not publish outbox message {MessageId}.", message.Id);
                message.Status = OutboxMessageStatus.Pending;
                message.PublishedAt = null;
            }
        }
    }
}
