namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;

/// <summary>
/// Thrown when the aggregate state is still empty after every handler has run.
/// </summary>
/// <remarks>
/// Raised by <see cref="EventHandlerBus{T}.ProcessAsync"/>; <see cref="EventProcessorBase{T}"/> logs it and skips the
/// event.
/// </remarks>
public class EmptyStateAfterHandlerBusException() : Exception(
    "The state remained unaffected after all handlers were executed.");
