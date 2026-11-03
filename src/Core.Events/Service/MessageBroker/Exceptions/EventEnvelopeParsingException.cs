using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Service.MessageBroker.Exceptions;

/// <summary>
/// Thrown when an inbound message body cannot be parsed into an event envelope.
/// </summary>
public class EventEnvelopeParsingException(Type envelopeType, Exception? innerException = null)
    : MessageBrokerParsingException($"Failed to parse the inbound message body into {envelopeType.Name}", innerException);
