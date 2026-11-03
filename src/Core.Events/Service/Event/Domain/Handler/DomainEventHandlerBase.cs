using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Extension;

namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler;

using Event = Entity.Event.Domain.Event;

/// <summary>
/// Base class for event handlers. The set of supported aggregate/event types is expressed over
/// strings (aggregate description -> supported event descriptions) so the pipeline stays decoupled
/// from concrete enums. Subclasses may still register support using enums through the
/// <see cref="Supports{TAgg,TEvt}"/> helper.
/// </summary>
public abstract class DomainEventHandlerBase<TDomainModelEntity>(
    DomainEventHandlerBase<TDomainModelEntity>.SupportedAggregateTypes
        supportedAggregateTypes,
    bool isStateInitializer = false)
    : IDomainEventHandler<TDomainModelEntity>
    where TDomainModelEntity : IEntity
{
    /// <summary>
    /// Maps a supported aggregate type (description string) to the set of supported event type
    /// descriptions. A <c>null</c> value means every event type of the aggregate is supported.
    /// </summary>
    public class SupportedAggregateTypes : Dictionary<string, IReadOnlyCollection<string>?>
    {
        /// <summary>
        /// Registers support for the given aggregate and event enums, storing their description strings.
        /// </summary>
        public SupportedAggregateTypes Support<TAgg, TEvt>(TAgg aggregate, params TEvt[] events)
            where TAgg : Enum
            where TEvt : Enum
        {
            this[aggregate.ToDescription()] =
                events.Length == 0 ? null : events.Select(e => e.ToDescription()).ToArray();
            return this;
        }
    }

    /// <summary>
    /// Builds a <see cref="SupportedAggregateTypes"/> registration from enums for a single aggregate.
    /// </summary>
    protected static SupportedAggregateTypes Supports<TAgg, TEvt>(TAgg aggregate, params TEvt[] events)
        where TAgg : Enum
        where TEvt : Enum
        => new SupportedAggregateTypes().Support(aggregate, events);

    protected bool CanHandleEventType(Event domainEvent)
    {
        if (!supportedAggregateTypes.TryGetValue(domainEvent.TypeAggregate, out var supportedEventTypes))
        {
            return false;
        }

        return supportedEventTypes is not { Count: > 0 } ||
               supportedEventTypes.Any(x => x == domainEvent.TypeEvent);
    }

    public virtual bool CanHandleEvent(Event domainEvent, TDomainModelEntity? entity)
    {
        if (!CanHandleEventType(domainEvent))
        {
            return false;
        }

        return isStateInitializer switch {
            false when entity == null => false,
            _ => true
        };
    }

    public abstract Task<TDomainModelEntity> HandleAsync(Event domainEvent,
                                                         TDomainModelEntity? entity,
                                                         CancellationToken? cancellationToken = null);
}
