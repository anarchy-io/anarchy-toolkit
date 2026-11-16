namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

/// <summary>
/// Thrown when one or more messages of a batch publication fail.
/// </summary>
/// <param name="failedMessageCount">The number of messages that failed to publish.</param>
/// <param name="innerException">The underlying broker failure, if any.</param>
/// <param name="destination">The destination the batch was published to, included in the message when provided.</param>
public class MessageBatchPublishException(int failedMessageCount, Exception? innerException = null, string? destination = null)
    : MessageBrokerPublisherException(
        $"Failed to publish {failedMessageCount} outbound message(s)" + (destination is null ? "" : $" to '{destination}'"),
        innerException)
{
    /// <summary>
    /// Gets the number of messages that failed to publish.
    /// </summary>
    public int FailedMessageCount { get; } = failedMessageCount;
}
