using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler;

using Event = Entity.Event.Domain.Event;

/// <summary>
/// Base class for handlers that mutate existing aggregate state, but are optional in nature: when the state is
/// absent the handler does not run and throws <see cref="NoStateFoundForOptionalEventHandlerException"/>, allowing
/// the caller to handle (or ignore) the absence of state appropriately.
/// </summary>
public abstract class OptionalStateMutationDomainEventHandlerBase<TDomainModelEntity>(
    DomainEventHandlerBase<TDomainModelEntity>.SupportedAggregateTypes
        supportedAggregateTypes)
    : StateMutationDomainEventHandlerBase<TDomainModelEntity>(supportedAggregateTypes)
    where TDomainModelEntity : IEntity
{
    public override async Task<TDomainModelEntity> HandleAsync(Event domainEvent,
                                                               TDomainModelEntity? modelEntity,
                                                               CancellationToken? cancellationToken = null)
    {
        return modelEntity == null
            ? throw new NoStateFoundForOptionalEventHandlerException()
            : await HandleAsync(modelEntity, domainEvent, cancellationToken);
    }

    public override bool CanHandleEvent(Event domainEvent, TDomainModelEntity? entity)
    {
        return CanHandleEventType(domainEvent);
    }
}
