using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Envelopes.Exceptions;

/// <summary>
/// Thrown when an inbound message body cannot be parsed into an event envelope.
/// </summary>
/// <param name="envelopeType">The envelope type the body was being parsed into.</param>
/// <param name="innerException">The underlying deserialization failure, if any.</param>
public class EventEnvelopeParsingException(Type envelopeType, Exception? innerException = null)
    : MessageBrokerParsingException($"Failed to parse the inbound message body into {envelopeType.Name}", innerException);
