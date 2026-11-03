using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Entity.Event.Exceptions;

/// <summary>
/// Thrown when a null envelope payload is cast to a concrete type.
/// </summary>
public class EmptyEnvelopePayloadParseException<T>()
    : InvalidFormatException($"Cannot cast a null event envelope payload to {typeof(T).Name}");
