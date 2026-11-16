using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Entity.Event.Exceptions;

/// <summary>
/// Thrown when an event envelope is constructed with an empty identity value.
/// </summary>
/// <param name="propertyName">
/// The envelope property that was empty (<c>TypeAggregate</c>, <c>AggregateId</c> or <c>TypeEvent</c>).
/// </param>
public class InvalidEventEnvelopeValueException(string propertyName)
    : EmptyArgumentException(propertyName, $"Event envelope {propertyName} cannot be null or empty.");
