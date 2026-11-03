using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Entity.Event.Exceptions;

/// <summary>
/// Thrown when a serialized envelope payload is not valid JSON.
/// </summary>
public class EnvelopePayloadDeserializationException(Exception innerException)
    : InvalidFormatException("Failed to deserialize the event envelope payload", innerException);
