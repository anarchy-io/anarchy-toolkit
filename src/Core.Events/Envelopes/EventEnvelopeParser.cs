using Anarchy.Toolkit.Core.Events.Envelopes.Exceptions;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Newtonsoft.Json;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Deserializes an inbound broker message body into a typed event envelope using Newtonsoft.Json.
/// </summary>
/// <remarks>
/// Newtonsoft.Json is used deliberately because it can bind the primary constructors of
/// <see cref="DomainEventEnvelope"/> and <see cref="PersistedDomainEvent"/>. The parser is
/// stateless and thread-safe.
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
        try
        {
            // Deserializing with Newtonsoft.Json intentionally for relaxed requirements on polymorphic types.
            // Do not change to System.Text.Json without careful consideration on available constructors on
            // the DomainEventEnvelope / PersistedDomainEvent types
            var result = JsonConvert.DeserializeObject<TEventEnvelope>(inboundMessage.Message)
                         ?? throw new EventEnvelopeParsingException(typeof(TEventEnvelope));
            return Task.FromResult(result);
        }
        catch (JsonException ex)
        {
            throw new EventEnvelopeParsingException(typeof(TEventEnvelope), ex);
        }
    }
}
