using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler.Exceptions;

/// <summary>
/// Thrown when a state-creation handler is invoked for an aggregate whose state already exists.
/// </summary>
/// <param name="aggregateId">The identifier of the aggregate whose state already exists.</param>
/// <param name="typeEvent">The type of the event being handled.</param>
/// <param name="handlerType">The handler that detected the condition.</param>
public class StateAlreadyInitializedEventHandlerException(string aggregateId, string typeEvent, Type handlerType)
    : UnableToHandleEventException("The aggregate state has already been initialized before the handler was called.")
{
    /// <summary>The identifier of the aggregate whose state already exists.</summary>
    public readonly string AggregateId = aggregateId;

    /// <summary>The type of the event being handled.</summary>
    public readonly string TypeEvent = typeEvent;

    /// <summary>The name of the handler type that detected the condition.</summary>
    public readonly string HandlerName = handlerType.Name;
}
