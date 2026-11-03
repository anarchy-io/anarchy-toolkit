namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

/// <summary>
/// Thrown when one or more messages of a batch publication fail.
/// </summary>
public class MessageBatchPublishException(int failedMessageCount, Exception? innerException = null, string? destination = null)
    : MessageBrokerPublisherException(
        $"Failed to publish {failedMessageCount} outbound message(s)" + (destination is null ? "" : $" to '{destination}'"),
        innerException)
{
    public int FailedMessageCount { get; } = failedMessageCount;
}
