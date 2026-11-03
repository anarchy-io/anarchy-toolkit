using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;

/// <summary>
/// Thrown when no handler in the bus reported being able to process an incoming event.
/// </summary>
public class NoHandlersForEventException(string eventType = "") : NotFoundException(
    $"No handlers in the collection have reported being able to process incoming event {eventType}");
