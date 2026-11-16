using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Entity.Event.Exceptions;

/// <summary>
/// Thrown when a serialized envelope payload is not valid JSON.
/// </summary>
/// <remarks>Raised by the <see cref="EventEnvelopeBase{TPayload}"/> constructor for string payloads.</remarks>
/// <param name="innerException">The underlying JSON deserialization failure.</param>
public class EnvelopePayloadDeserializationException(Exception innerException)
    : InvalidFormatException("Failed to deserialize the event envelope payload", innerException);
