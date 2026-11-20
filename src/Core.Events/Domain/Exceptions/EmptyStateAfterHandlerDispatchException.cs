namespace Anarchy.Toolkit.Core.Events.Domain.Exceptions;

/// <summary>
/// Thrown when the aggregate state is still empty after every handler has run.
/// </summary>
/// <remarks>
/// Raised by <see cref="DomainEventHandlerDispatcher{T}.ProcessAsync"/>; <see cref="DomainEventProcessorBase{T}"/> logs it and skips the
/// event.
/// </remarks>
public class EmptyStateAfterHandlerDispatchException() : Exception(
    "The state remained unaffected after all handlers were executed.");
