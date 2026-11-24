using Anarchy.Toolkit.Core.Events.Envelopes.Exceptions;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Newtonsoft.Json;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>Maps event envelope JSON to and from broker-neutral messages.</summary>
/// <remarks>
/// The message body is authoritative. Broker routing, delivery identity and other broker metadata are not part of
/// the event envelope or its CloudEvents representation.
/// </remarks>
public static class EventEnvelopeMessageMapper
{
    /// <summary>Builds a message whose body contains the complete event envelope.</summary>
    /// <param name="envelope">The event to publish.</param>
    /// <param name="destination">The broker destination, not the source of the event.</param>
    /// <param name="messageId">The caller-assigned message ID. Reuse it for retries of the same publication.</param>
    /// <param name="partitionKey">An optional override for the default partition key, the event subject.</param>
    /// <param name="deduplicationId">An optional override for the default deduplication ID, <paramref name="messageId"/>.</param>
    /// <param name="attributes">Optional broker-only attributes. They are not written into the event body.</param>
    /// <returns>A message ready for publishing.</returns>
    /// <remarks>
    /// Broker attributes start with the event's custom attributes. Explicit message attributes take precedence over
    /// custom attributes, and the projected <c>AggregateType</c>, <c>Subject</c> and <c>Type</c> take precedence over
    /// both. Keys are compared ordinally. Collisions affect only the broker attributes, not the serialized envelope.
    /// Neither the persisted event ID nor a CloudEvent ID is inferred as the message ID.
    /// </remarks>
    public static OutboundMessage ToOutboundMessage(
        IEventEnvelope envelope,
        string destination,
        string messageId,
        string? partitionKey = null,
        string? deduplicationId = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var messageAttributes = new Dictionary<string, string>(envelope.CustomAttributes, StringComparer.Ordinal);
        if (attributes is not null)
        {
            foreach (var (key, value) in attributes)
                messageAttributes[key] = value;
        }

        messageAttributes[nameof(IEventEnvelope.AggregateType)] = envelope.AggregateType;
        messageAttributes[nameof(IEventEnvelope.Subject)] = envelope.Subject;
        messageAttributes[nameof(IEventEnvelope.Type)] = envelope.Type;

        return new OutboundMessage(
            messageId,
            destination,
            JsonConvert.SerializeObject(envelope),
            partitionKey ?? envelope.Subject,
            deduplicationId ?? messageId,
            messageAttributes);
    }

    /// <summary>Builds an inbound message containing the complete envelope and explicit broker delivery metadata.</summary>
    /// <param name="envelope">The event represented by the message.</param>
    /// <param name="source">The broker receive location, not the producer's event source.</param>
    /// <param name="messageId">The broker-assigned message ID.</param>
    /// <param name="brokerReference">The broker delivery reference.</param>
    /// <param name="partitionKey">An optional override for the event subject.</param>
    /// <param name="attributes">Optional broker-only attributes; projected event fields take precedence.</param>
    /// <returns>A message carrying the serialized envelope and its projected attributes.</returns>
    public static InboundMessage ToInboundMessage(
        IEventEnvelope envelope,
        string source,
        string messageId,
        string brokerReference,
        string? partitionKey = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        var outgoing = ToOutboundMessage(envelope, source, messageId, partitionKey, attributes: attributes);
        return new InboundMessage(messageId, brokerReference, outgoing.Message, source, outgoing.PartitionKey, outgoing.Attributes);
    }

    /// <summary>Reads an event envelope from the message body.</summary>
    /// <typeparam name="TEventEnvelope">The concrete envelope type expected in the body.</typeparam>
    /// <param name="inboundMessage">The broker message containing the envelope.</param>
    /// <returns>The envelope, including its original custom attributes.</returns>
    /// <remarks>
    /// Broker attributes (including any projected event attributes), source, partition key, message ID and delivery
    /// reference are not copied into the envelope. Use the inbound message directly if this metadata is needed.
    /// Select <see cref="PersistedDomainEvent"/> when the serialized storage ID is needed.
    /// </remarks>
    /// <exception cref="EventEnvelopeParsingException">The message body cannot be deserialized as the expected envelope.</exception>
    public static TEventEnvelope ToEnvelope<TEventEnvelope>(InboundMessage inboundMessage)
        where TEventEnvelope : class, IEventEnvelope
    {
        ArgumentNullException.ThrowIfNull(inboundMessage);

        return DeserializeEnvelope<TEventEnvelope>(inboundMessage.Message);
    }

    /// <summary>Reads an event envelope from an outbound message body.</summary>
    /// <typeparam name="TEventEnvelope">The concrete envelope type expected in the body.</typeparam>
    /// <param name="outboundMessage">The broker message containing the envelope.</param>
    /// <returns>The envelope, including its original custom attributes.</returns>
    /// <exception cref="EventEnvelopeParsingException">The message body cannot be deserialized as the expected envelope.</exception>
    public static TEventEnvelope ToEnvelope<TEventEnvelope>(OutboundMessage outboundMessage)
        where TEventEnvelope : class, IEventEnvelope
    {
        ArgumentNullException.ThrowIfNull(outboundMessage);
        return DeserializeEnvelope<TEventEnvelope>(outboundMessage.Message);
    }

    private static TEventEnvelope DeserializeEnvelope<TEventEnvelope>(string body) where TEventEnvelope : class, IEventEnvelope
    {
        try
        {
            return JsonConvert.DeserializeObject<TEventEnvelope>(body,
                       new JsonSerializerSettings { DateParseHandling = DateParseHandling.None })
                   ?? throw new EventEnvelopeParsingException(typeof(TEventEnvelope));
        }
        catch (JsonException ex)
        {
            throw new EventEnvelopeParsingException(typeof(TEventEnvelope), ex);
        }
    }
}
