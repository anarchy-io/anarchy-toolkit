using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Entity.Event.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anarchy.Toolkit.Core.Events.Entity.Event;

/// <summary>
/// Base type for event envelopes. Stores the aggregate/event identity as strings and
/// carries an opaque payload that can be cast to a concrete type on demand.
/// </summary>
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
    public string TypeAggregate { get; } = typeAggregate.IsNullOrEmpty() ?
        throw new InvalidEventEnvelopeValueException(nameof(TypeAggregate)) :
        typeAggregate.Trim();

    public string AggregateId { get; } = aggregateId.IsNullOrEmpty() ?
        throw new InvalidEventEnvelopeValueException(nameof(AggregateId)) :
        aggregateId.Trim();

    public string TypeEvent { get; } = typeEvent.IsNullOrEmpty() ?
        throw new InvalidEventEnvelopeValueException(nameof(TypeEvent)) :
        typeEvent.Trim();

    public DateTimeOffset Moment { get; } = moment ?? DateHelper.Now();

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

    public abstract EventEnvelopeBase<TPayload> DeepClone();
}
