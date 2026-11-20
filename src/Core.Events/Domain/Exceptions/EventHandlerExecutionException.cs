namespace Anarchy.Toolkit.Core.Events.Domain.Exceptions;

/// <summary>
/// Thrown when a handler fails while processing an event. The original failure is the inner exception.
/// </summary>
/// <remarks>Raised by <see cref="DomainEventHandlerDispatcher{T}.ProcessAsync"/>.</remarks>
/// <param name="typeEvent">The type of the event being processed.</param>
/// <param name="handlerType">The handler that failed.</param>
/// <param name="innerException">The exception thrown by the handler.</param>
public class EventHandlerExecutionException(string typeEvent, Type handlerType, Exception innerException)
    : UnableToHandleEventException($"Failed to process event {typeEvent} using handler {handlerType.Name}", innerException)
{
    /// <summary>
    /// Gets the type of the event being processed.
    /// </summary>
    public string TypeEvent { get; } = typeEvent;

    /// <summary>
    /// Gets the name of the handler type that failed.
    /// </summary>
    public string HandlerName { get; } = handlerType.Name;
}
