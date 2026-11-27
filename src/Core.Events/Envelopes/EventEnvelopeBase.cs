using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Envelopes.Exceptions;
using Anarchy.Toolkit.Core.Helpers.Dates;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace Anarchy.Toolkit.Core.Events.Envelopes;

/// <summary>
/// Base type for event envelopes. Stores the aggregate/event identity as strings and
/// carries an opaque payload that can be cast to a concrete type on demand.
/// </summary>
/// <remarks>
/// <para>
/// Identity values are validated and trimmed on construction. When <paramref name="data"/> is a
/// <see cref="string"/> it is treated as JSON and deserialized with Newtonsoft.Json (typically into a
/// <see cref="JObject"/>); any other object is stored as-is. Use <see cref="CastData{TCast}"/> to obtain a typed
/// view of the payload.
/// </para>
/// <para>Concrete implementations are <see cref="DomainEventEnvelope"/> and <see cref="PersistedDomainEvent"/>.</para>
/// </remarks>
/// <typeparam name="TPayload">The payload marker type associated with the envelope.</typeparam>
/// <param name="aggregateType">The aggregate type identifier (e.g. an enum description). Required.</param>
/// <param name="subject">The identifier of the aggregate instance the event belongs to. Required.</param>
/// <param name="type">The event type identifier (e.g. an enum description). Required.</param>
/// <param name="data">The event data: a JSON string, an object, or <see langword="null"/>.</param>
/// <param name="time">When the event happened. Defaults to the current UTC time.</param>
/// <param name="customAttributes">Optional event metadata. The envelope stores a read-only snapshot.</param>
/// <exception cref="InvalidEventEnvelopeValueException">
/// <paramref name="aggregateType"/>, <paramref name="subject"/> or <paramref name="type"/> is null or whitespace.
/// </exception>
/// <exception cref="EnvelopePayloadDeserializationException">
/// <paramref name="data"/> is a string that is not valid JSON.
/// </exception>
public abstract class EventEnvelopeBase<TPayload>(
    string aggregateType,
    string subject,
    string type,
    object? data = null,
    DateTimeOffset? time = null,
    IReadOnlyDictionary<string, string>? customAttributes = null)
    : IDeepCloneableEntity<EventEnvelopeBase<TPayload>>,
      IEventEnvelope
    where TPayload : IEventEnvelopePayload
{
    /// <inheritdoc/>
    public string AggregateType { get; } = string.IsNullOrWhiteSpace(aggregateType) ?
        throw new InvalidEventEnvelopeValueException(nameof(AggregateType)) :
        aggregateType.Trim();

    /// <inheritdoc/>
    public string Subject { get; } = string.IsNullOrWhiteSpace(subject) ?
        throw new InvalidEventEnvelopeValueException(nameof(Subject)) :
        subject.Trim();

    /// <inheritdoc/>
    public string Type { get; } = string.IsNullOrWhiteSpace(type) ?
        throw new InvalidEventEnvelopeValueException(nameof(Type)) :
        type.Trim();

    /// <inheritdoc/>
    public DateTimeOffset Time { get; } = time ?? DateHelper.Now();

    /// <inheritdoc/>
    /// <remarks>String payloads passed to the constructor are exposed in their deserialized (JSON DOM) form.</remarks>
    public object? Data { get; } = data switch {
        JValue { Type: JTokenType.Null } => null,
        string s => DeserializeData(s),
        _ => data
    };

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> CustomAttributes { get; } = customAttributes is null
        ? ReadOnlyDictionary<string, string>.Empty
        : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(customAttributes, StringComparer.Ordinal));

    private static JToken? DeserializeData(string json)
    {
        try
        {
            var token = JsonConvert.DeserializeObject<JToken>(json,
                new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
            return token is JValue { Type: JTokenType.Null } ? null : token;
        }
        catch (JsonException ex)
        {
            throw new EnvelopePayloadDeserializationException(ex);
        }
    }

    /// <summary>
    /// Converts <see cref="Data"/> into <typeparamref name="TCast"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="JToken"/> payload is converted with <see cref="JToken.ToObject{T}()"/>; any other payload is
    /// cast directly.
    /// </remarks>
    /// <typeparam name="TCast">The target payload type.</typeparam>
    /// <returns>The typed payload.</returns>
    /// <exception cref="EmptyEnvelopePayloadParseException{T}"><see cref="Data"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidEnvelopePayloadParseException{T}">
    /// The payload cannot be converted to <typeparamref name="TCast"/> or the conversion yields
    /// <see langword="null"/>; the original failure is the inner exception.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    /// var payload = envelope.CastData&lt;OrderCreatedPayload&gt;();
    /// </code>
    /// </example>
    public TCast CastData<TCast>()
    {
        if (Data is null) throw new EmptyEnvelopePayloadParseException<TCast>();

        try
        {
            return (Data is TCast typedData ? typedData : Data is JToken token ? token.ToObject<TCast>() : (TCast)Data) ??
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
