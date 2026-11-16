namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

/// <summary>
/// Thrown when a single outbound message cannot be published.
/// </summary>
/// <param name="innerException">The underlying broker failure.</param>
public class MessagePublishException(Exception innerException)
    : MessageBrokerPublisherException("Failed to publish the outbound message", innerException);
