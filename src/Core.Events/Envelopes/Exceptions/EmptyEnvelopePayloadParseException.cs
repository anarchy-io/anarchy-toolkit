using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Envelopes.Exceptions;

/// <summary>
/// Thrown when a null envelope payload is cast to a concrete type.
/// </summary>
/// <remarks>Raised by <see cref="EventEnvelopeBase{TPayload}.CastData{TCast}"/>.</remarks>
/// <typeparam name="T">The type the payload was being cast to.</typeparam>
public class EmptyEnvelopePayloadParseException<T>()
    : InvalidFormatException($"Cannot cast a null event envelope payload to {typeof(T).Name}");
