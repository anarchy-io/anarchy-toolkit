using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler;

/// <summary>
/// Base class for handlers that create (initialize) aggregate state from an event.
/// </summary>
public abstract class StateCreationDomainEventHandlerBase<TDomainModelEntity>(
    DomainEventHandlerBase<TDomainModelEntity>.SupportedAggregateTypes
        supportedAggregateTypes)
    : DomainEventHandlerBase<TDomainModelEntity>(supportedAggregateTypes, isStateInitializer: true)
    where TDomainModelEntity : IEntity;
