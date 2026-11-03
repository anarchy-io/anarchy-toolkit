namespace Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

/// <summary>
/// Thrown when a single outbound message cannot be published.
/// </summary>
public class MessagePublishException(Exception innerException)
    : MessageBrokerPublisherException("Failed to publish the outbound message", innerException);
