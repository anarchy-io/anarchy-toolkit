using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Messaging.Aws.Service.MessageBroker.Aws.Publisher.RequestBuilder.Exceptions;

/// <summary>
/// Thrown when a message exceeds the broker size limit and compression was disabled for it.
/// </summary>
public class UncompressedMessageTooLargeException(long maxSizeBytes)
    : InvalidArgumentException($"Message body is too large to be sent without compression (limit: {maxSizeBytes} bytes)")
{
    public long MaxSizeBytes { get; } = maxSizeBytes;
}
