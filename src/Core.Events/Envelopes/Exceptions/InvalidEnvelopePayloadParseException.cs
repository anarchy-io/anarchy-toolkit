using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Envelopes.Exceptions;

/// <summary>
/// Thrown when an envelope payload cannot be parsed into the requested type.
/// </summary>
/// <remarks>Raised by <see cref="EventEnvelopeBase{TPayload}.CastPayload{TCast}"/>.</remarks>
/// <typeparam name="T">The type the payload was being parsed into.</typeparam>
/// <param name="innerException">The original conversion failure, if any.</param>
public class InvalidEnvelopePayloadParseException<T>(Exception? innerException = null)
    : InvalidFormatException($"Failed to parse the event envelope payload using {typeof(T).Name}", innerException);
