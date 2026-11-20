using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Extension;

namespace Anarchy.Toolkit.Core.Events.Domain.Handler;

/// <summary>
/// Base class for event handlers. The set of supported aggregate/event types is expressed over
/// strings (aggregate description -> supported event descriptions) so the pipeline stays decoupled
/// from concrete enums. Subclasses may still register support using enums through the
/// <see cref="Supports{TAgg,TEvt}"/> helper.
/// </summary>
/// <remarks>
/// Prefer one of the specialised bases: <see cref="StateCreationDomainEventHandlerBase{TDomainModelEntity}"/>,
/// <see cref="StateMutationDomainEventHandlerBase{TDomainModelEntity}"/> or
/// <see cref="OptionalStateMutationDomainEventHandlerBase{TDomainModelEntity}"/>.
/// </remarks>
/// <typeparam name="TDomainModelEntity">The aggregate state type.</typeparam>
/// <param name="supportedAggregateTypes">The aggregate/event types this handler supports.</param>
/// <param name="isStateInitializer">
/// When <see langword="true"/>, the handler can run without existing state; when <see langword="false"/> (default),
/// <see cref="CanHandleEvent"/> returns <see langword="false"/> if state is <see langword="null"/>.
/// </param>
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
    /// <remarks>An empty collection is treated the same as <c>null</c> (all event types supported).</remarks>
    /// <example>
    /// <code language="csharp">
    /// var supported = new SupportedAggregateTypes()
    ///     .Support(Aggregate.Order, OrderEvent.Created, OrderEvent.Paid)
    ///     .Support(Aggregate.Invoice, Array.Empty&lt;InvoiceEvent&gt;()); // every invoice event
    /// </code>
    /// </example>
    public class SupportedAggregateTypes : Dictionary<string, IReadOnlyCollection<string>?>
    {
        /// <summary>
        /// Registers support for the given aggregate and event enums, storing their description strings.
        /// </summary>
        /// <typeparam name="TAgg">The aggregate type enum.</typeparam>
        /// <typeparam name="TEvt">The event type enum.</typeparam>
        /// <param name="aggregate">The supported aggregate type.</param>
        /// <param name="events">
        /// The supported event types; when empty, every event of <paramref name="aggregate"/> is supported.
        /// </param>
        /// <returns>This instance, for chaining. An existing entry for the same aggregate is replaced.</returns>
        /// <exception cref="Core.Extension.Exceptions.MissingEnumDescriptionException">
        /// An enum value has no description.
        /// </exception>
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
    /// <typeparam name="TAgg">The aggregate type enum.</typeparam>
    /// <typeparam name="TEvt">The event type enum.</typeparam>
    /// <param name="aggregate">The supported aggregate type.</param>
    /// <param name="events">The supported event types; when empty, every event of the aggregate is supported.</param>
    /// <returns>A new registration.</returns>
    protected static SupportedAggregateTypes Supports<TAgg, TEvt>(TAgg aggregate, params TEvt[] events)
        where TAgg : Enum
        where TEvt : Enum
        => new SupportedAggregateTypes().Support(aggregate, events);

    /// <summary>
    /// Checks whether the event's aggregate type and event type are among the supported types, ignoring state.
    /// </summary>
    /// <param name="domainEvent">The incoming event.</param>
    /// <returns><see langword="true"/> when the aggregate/event type combination is supported.</returns>
    protected bool CanHandleEventType(DomainEventEnvelope domainEvent)
    {
        if (!supportedAggregateTypes.TryGetValue(domainEvent.TypeAggregate, out var supportedEventTypes))
        {
            return false;
        }

        return supportedEventTypes is not { Count: > 0 } ||
               supportedEventTypes.Any(x => x == domainEvent.TypeEvent);
    }

    /// <summary>
    /// Determines whether this handler should process <paramref name="domainEvent"/> given the current state.
    /// </summary>
    /// <param name="domainEvent">The incoming event.</param>
    /// <param name="entity">The current aggregate state, or <see langword="null"/> when none exists yet.</param>
    /// <returns>
    /// <see langword="true"/> when the event type is supported and either state exists or the handler is a state
    /// initializer.
    /// </returns>
    public virtual bool CanHandleEvent(DomainEventEnvelope domainEvent, TDomainModelEntity? entity)
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

    /// <summary>
    /// Applies <paramref name="domainEvent"/> to the aggregate state.
    /// </summary>
    /// <param name="domainEvent">The event to apply.</param>
    /// <param name="entity">The current aggregate state, or <see langword="null"/> when none exists yet.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The resulting aggregate state.</returns>
    public abstract Task<TDomainModelEntity> HandleAsync(DomainEventEnvelope domainEvent,
                                                         TDomainModelEntity? entity,
                                                         CancellationToken? cancellationToken = null);
}
