using Anarchy.Toolkit.Core.Events.Envelopes.Exceptions;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Deserializes an inbound broker message body into a typed event envelope using Newtonsoft.Json.
/// </summary>
/// <remarks>
/// Uses <see cref="EventEnvelopeMessageMapper"/> to deserialize the envelope body without mapping broker metadata.
/// The parser is stateless and thread-safe.
/// </remarks>
/// <typeparam name="TInboundMessage">The broker-specific inbound message type.</typeparam>
/// <typeparam name="TEventEnvelope">The envelope type to produce.</typeparam>
/// <example>
/// <code language="csharp">
/// var parser = new EventEnvelopeParser&lt;InboundMessage, DomainEventEnvelope&gt;();
/// var envelope = await parser.FromInboundMessage(message, cancellationToken);
/// </code>
/// </example>
public class EventEnvelopeParser<TInboundMessage, TEventEnvelope> : IEventEnvelopeParser<TInboundMessage, TEventEnvelope>
    where TInboundMessage : InboundMessage where TEventEnvelope : class, IEventEnvelope
{
    /// <inheritdoc/>
    /// <exception cref="EventEnvelopeParsingException">
    /// The message body is not valid JSON, cannot be bound to <typeparamref name="TEventEnvelope"/>, or deserializes
    /// to <see langword="null"/>.
    /// </exception>
    public Task<TEventEnvelope> FromInboundMessage(TInboundMessage inboundMessage,
                                                   CancellationToken? cancellationToken = null)
    {
        return Task.FromResult(EventEnvelopeMessageMapper.ToEnvelope<TEventEnvelope>(inboundMessage));
    }
}
