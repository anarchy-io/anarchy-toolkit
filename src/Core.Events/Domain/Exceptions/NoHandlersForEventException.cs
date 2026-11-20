using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Domain.Exceptions;

/// <summary>
/// Thrown when no handler in the bus reported being able to process an incoming event.
/// </summary>
/// <remarks>Raised by <see cref="DomainEventHandlerDispatcher{T}.ProcessAsync"/>.</remarks>
/// <param name="eventType">The type of the unhandled event.</param>
public class NoHandlersForEventException(string eventType = "") : NotFoundException(
    $"No handlers in the collection have reported being able to process incoming event {eventType}");
