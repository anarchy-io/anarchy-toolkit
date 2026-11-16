using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler;

using Event = Entity.Event.Domain.Event;

/// <summary>
/// Base class for handlers that mutate existing aggregate state, but are optional in nature: when the state is
/// absent the handler does not run and throws <see cref="NoStateFoundForOptionalEventHandlerException"/>, allowing
/// the caller to handle (or ignore) the absence of state appropriately.
/// </summary>
/// <remarks>
/// Unlike <see cref="StateMutationDomainEventHandlerBase{TDomainModelEntity}"/>, <see cref="CanHandleEvent"/> only
/// checks the event type, so the handler is selected even without state. <see cref="EventHandlerBus{T}"/> treats the
/// resulting <see cref="NoStateFoundForOptionalEventHandlerException"/> as "handled", which avoids a
/// <see cref="NoHandlersForEventException"/> for events that may legitimately arrive before the aggregate exists.
/// </remarks>
/// <typeparam name="TDomainModelEntity">The aggregate state type.</typeparam>
/// <param name="supportedAggregateTypes">The aggregate/event types this handler supports.</param>
public abstract class OptionalStateMutationDomainEventHandlerBase<TDomainModelEntity>(
    DomainEventHandlerBase<TDomainModelEntity>.SupportedAggregateTypes
        supportedAggregateTypes)
    : StateMutationDomainEventHandlerBase<TDomainModelEntity>(supportedAggregateTypes)
    where TDomainModelEntity : IEntity
{
    /// <summary>
    /// Delegates to the protected state-mutation overload when state exists; otherwise signals that there is
    /// nothing to mutate.
    /// </summary>
    /// <param name="domainEvent">The event to apply.</param>
    /// <param name="modelEntity">The current aggregate state, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The updated aggregate state.</returns>
    /// <exception cref="NoStateFoundForOptionalEventHandlerException">
    /// <paramref name="modelEntity"/> is <see langword="null"/>.
    /// </exception>
    public override async Task<TDomainModelEntity> HandleAsync(Event domainEvent,
                                                               TDomainModelEntity? modelEntity,
                                                               CancellationToken? cancellationToken = null)
    {
        return modelEntity == null
            ? throw new NoStateFoundForOptionalEventHandlerException()
            : await HandleAsync(modelEntity, domainEvent, cancellationToken);
    }

    /// <summary>
    /// Determines whether the handler supports the event's aggregate and event type, regardless of whether state
    /// exists.
    /// </summary>
    /// <param name="domainEvent">The incoming event.</param>
    /// <param name="entity">The current aggregate state (ignored).</param>
    /// <returns><see langword="true"/> when the aggregate/event type is supported.</returns>
    public override bool CanHandleEvent(Event domainEvent, TDomainModelEntity? entity)
    {
        return CanHandleEventType(domainEvent);
    }
}
