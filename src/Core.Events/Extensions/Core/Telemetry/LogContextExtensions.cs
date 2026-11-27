using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Telemetry;

namespace Anarchy.Toolkit.Core.Events.Extensions.Core.Telemetry;

/// <summary>
/// Log-context helpers that enrich structured logs with event envelope properties.
/// </summary>
/// <remarks>
/// The pushed properties are <c>AggregateId</c>, <c>TypeAggregate</c> and <c>TypeEvent</c>.
/// <see cref="Domain.DomainEventProcessorBase{T}"/> uses <c>WithEvent</c> around each processed event.
/// </remarks>
/// <example>
/// <code language="csharp">
/// using (telemetry.WithEvent(domainEvent))
/// {
///     // every log written here carries AggregateId, TypeAggregate and TypeEvent
/// }
/// </code>
/// </example>
public static class LogContextExtensions
{
    /// <summary>
    /// Builds the log-context properties describing <paramref name="domainEvent"/>.
    /// </summary>
    /// <param name="domainEvent">The event envelope to describe.</param>
    /// <returns>The <c>AggregateId</c>, <c>TypeAggregate</c> and <c>TypeEvent</c> properties.</returns>
    public static KeyValuePair<string, object?>[] ForEvent(IEventEnvelope domainEvent)
    {
        return [
            new("AggregateId", domainEvent.Subject),
            new("TypeAggregate", domainEvent.AggregateType),
            new("TypeEvent", domainEvent.Type)
        ];
    }

    /// <param name="telemetry">The telemetry facade to enrich.</param>
    extension(AppTelemetry telemetry)
    {
        /// <summary>
        /// Pushes the properties from <see cref="ForEvent"/> onto the log context.
        /// </summary>
        /// <param name="domainEvent">The event envelope to describe.</param>
        /// <returns>A handle that removes the properties when disposed.</returns>
        public IDisposable WithEvent(IEventEnvelope domainEvent)
        {
            return telemetry.LogContext.PushProperties(ForEvent(domainEvent));
        }
    }
}
