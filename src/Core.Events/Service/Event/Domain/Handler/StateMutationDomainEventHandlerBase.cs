using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler;

using Event = Entity.Event.Domain.Event;

/// <summary>
/// Base class for handlers that mutate existing aggregate state. Requires the state to be present.
/// </summary>
public abstract class StateMutationDomainEventHandlerBase<TDomainModelEntity>(
    DomainEventHandlerBase<TDomainModelEntity>.SupportedAggregateTypes
        supportedAggregateTypes)
    : DomainEventHandlerBase<TDomainModelEntity>(supportedAggregateTypes, isStateInitializer: false)
    where TDomainModelEntity : IEntity
{
    public override async Task<TDomainModelEntity> HandleAsync(Event domainEvent,
                                                               TDomainModelEntity? modelEntity,
                                                               CancellationToken? cancellationToken = null)
    {
        ArgumentNullException.ThrowIfNull(modelEntity);

        return await HandleAsync(modelEntity, domainEvent, cancellationToken);
    }

    protected abstract Task<TDomainModelEntity> HandleAsync(TDomainModelEntity modelEntity, Event domainEvent,
                                                            CancellationToken? cancellationToken = null);
}
