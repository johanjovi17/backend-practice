using System.Threading.Channels;
using BackendPractice.Models;

namespace BackendPractice.Services;

public sealed class EnrollmentMessageQueue
{
    private readonly Channel<EnrollmentCreatedMessage> _channel =
        Channel.CreateUnbounded<EnrollmentCreatedMessage>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(EnrollmentCreatedMessage message, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<EnrollmentCreatedMessage> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
