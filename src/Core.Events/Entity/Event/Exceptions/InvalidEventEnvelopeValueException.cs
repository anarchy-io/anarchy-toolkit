using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Entity.Event.Exceptions;

/// <summary>
/// Thrown when an event envelope is constructed with an empty identity value.
/// </summary>
public class InvalidEventEnvelopeValueException(string propertyName)
    : EmptyArgumentException(propertyName, $"Event envelope {propertyName} cannot be null or empty.");
