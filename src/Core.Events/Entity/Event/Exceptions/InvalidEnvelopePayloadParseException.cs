using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Entity.Event.Exceptions;

/// <summary>
/// Thrown when an envelope payload cannot be parsed into the requested type.
/// </summary>
public class InvalidEnvelopePayloadParseException<T>(Exception? innerException = null)
    : InvalidFormatException($"Failed to parse the event envelope payload using {typeof(T).Name}", innerException);
