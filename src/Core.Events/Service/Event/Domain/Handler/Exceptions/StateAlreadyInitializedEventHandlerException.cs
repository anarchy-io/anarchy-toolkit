using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler.Exceptions;

/// <summary>
/// Thrown when a state-creation handler is invoked for an aggregate whose state already exists.
/// </summary>
public class StateAlreadyInitializedEventHandlerException(string aggregateId, string typeEvent, Type handlerType)
    : UnableToHandleEventException("The aggregate state has already been initialized before the handler was called.")
{
    public readonly string AggregateId = aggregateId;
    public readonly string TypeEvent = typeEvent;
    public readonly string HandlerName = handlerType.Name;
}
