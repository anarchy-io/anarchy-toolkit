using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Envelopes.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Base type for event envelopes. Stores the aggregate/event identity as strings and
/// carries an opaque payload that can be cast to a concrete type on demand.
/// </summary>
/// <remarks>
/// <para>
/// Identity values are validated and trimmed on construction. When <paramref name="payload"/> is a
/// <see cref="string"/> it is treated as JSON and deserialized with Newtonsoft.Json (typically into a
/// <see cref="JObject"/>); any other object is stored as-is. Use <see cref="CastPayload{TCast}"/> to obtain a typed
/// view of the payload.
/// </para>
/// <para>Concrete implementations are <see cref="DomainEventEnvelope"/> and <see cref="PersistedDomainEvent"/>.</para>
/// </remarks>
/// <typeparam name="TPayload">The payload marker type associated with the envelope.</typeparam>
/// <param name="typeAggregate">The aggregate type identifier (e.g. an enum description). Required.</param>
/// <param name="aggregateId">The identifier of the aggregate instance the event belongs to. Required.</param>
/// <param name="typeEvent">The event type identifier (e.g. an enum description). Required.</param>
/// <param name="payload">The event payload: a JSON string, an object, or <see langword="null"/>.</param>
/// <param name="moment">When the event happened. Defaults to the current UTC time.</param>
/// <exception cref="InvalidEventEnvelopeValueException">
/// <paramref name="typeAggregate"/>, <paramref name="aggregateId"/> or <paramref name="typeEvent"/> is null or empty.
/// </exception>
/// <exception cref="EnvelopePayloadDeserializationException">
/// <paramref name="payload"/> is a string that is not valid JSON.
/// </exception>
public abstract class EventEnvelopeBase<TPayload>(
    string typeAggregate,
    string aggregateId,
    string typeEvent,
    object? payload = null,
    DateTimeOffset? moment = null)
    : IDeepCloneableEntity<EventEnvelopeBase<TPayload>>,
      IEventEnvelope
    where TPayload : IEventEnvelopePayload
{
    /// <inheritdoc/>
    public string TypeAggregate { get; } = typeAggregate.IsNullOrEmpty() ?
        throw new InvalidEventEnvelopeValueException(nameof(TypeAggregate)) :
        typeAggregate.Trim();

    /// <inheritdoc/>
    public string AggregateId { get; } = aggregateId.IsNullOrEmpty() ?
        throw new InvalidEventEnvelopeValueException(nameof(AggregateId)) :
        aggregateId.Trim();

    /// <inheritdoc/>
    public string TypeEvent { get; } = typeEvent.IsNullOrEmpty() ?
        throw new InvalidEventEnvelopeValueException(nameof(TypeEvent)) :
        typeEvent.Trim();

    /// <inheritdoc/>
    public DateTimeOffset Moment { get; } = moment ?? DateHelper.Now();

    /// <inheritdoc/>
    /// <remarks>String payloads passed to the constructor are exposed in their deserialized (JSON DOM) form.</remarks>
    public object? Payload { get; } = payload switch {
        null => null,
        string s => DeserializePayload(s),
        _ => payload
    };

    private static object? DeserializePayload(string s)
    {
        try
        {
            return JsonConvert.DeserializeObject<object>(s);
        }
        catch (JsonException ex)
        {
            throw new EnvelopePayloadDeserializationException(ex);
        }
    }

    /// <summary>
    /// Converts <see cref="Payload"/> into <typeparamref name="TCast"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="JObject"/> payload is converted with <see cref="JToken.ToObject{T}()"/>; any other payload is
    /// cast directly.
    /// </remarks>
    /// <typeparam name="TCast">The target payload type.</typeparam>
    /// <returns>The typed payload.</returns>
    /// <exception cref="EmptyEnvelopePayloadParseException{T}"><see cref="Payload"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidEnvelopePayloadParseException{T}">
    /// The payload cannot be converted to <typeparamref name="TCast"/> or the conversion yields
    /// <see langword="null"/>; the original failure is the inner exception.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    /// var payload = envelope.CastPayload&lt;OrderCreatedPayload&gt;();
    /// </code>
    /// </example>
    public TCast CastPayload<TCast>()
    {
        if (Payload is null) throw new EmptyEnvelopePayloadParseException<TCast>();

        try
        {
            return (Payload is JObject jPayload ? jPayload.ToObject<TCast>() : (TCast)Payload) ??
                   throw new EmptyEnvelopePayloadParseException<TCast>();
        }
        catch (Exception ex)
        {
            throw new InvalidEnvelopePayloadParseException<TCast>(ex);
        }
    }

    /// <inheritdoc/>
    public abstract EventEnvelopeBase<TPayload> DeepClone();
}
