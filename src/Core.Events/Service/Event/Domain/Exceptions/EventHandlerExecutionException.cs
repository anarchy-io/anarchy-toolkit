namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;

/// <summary>
/// Thrown when a handler fails while processing an event. The original failure is the inner exception.
/// </summary>
public class EventHandlerExecutionException(string typeEvent, Type handlerType, Exception innerException)
    : UnableToHandleEventException($"Failed to process event {typeEvent} using handler {handlerType.Name}", innerException)
{
    public string TypeEvent { get; } = typeEvent;
    public string HandlerName { get; } = handlerType.Name;
}
