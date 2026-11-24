using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Testing.Helper;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Envelopes;

public class GenericEventEnvelopeTest : EventEnvelopeTestBase
{
    public GenericEventEnvelopeTest() : this(null)
    {
    }

    protected GenericEventEnvelopeTest(
        string? expectedTypeAggregate,
        string? expectedTypeEvent = null,
        Dictionary<string, Type?>? expectedPayloadProperties = null)
        : base(expectedTypeAggregate ?? "generic", expectedTypeEvent ?? "some_type",
            expectedPayloadProperties ?? new Dictionary<string, Type?> {
                ["MyString"] = typeof(string),
                ["MyNumber"] = typeof(long),
                ["MyNull"] = null,
                ["MyBoolean"] = typeof(bool)
            })
    {
    }

    internal class GenericEventEnvelope(
        string typeAggregate,
        string aggregateId,
        string typeEvent,
        object? payload = null,
        DateTimeOffset? time = null)
        : EventEnvelopeBase<IEventEnvelopePayload>(typeAggregate, aggregateId, typeEvent, payload,
            time ?? TestHelper.LowPrecisionNow())
    {
        public override EventEnvelopeBase<IEventEnvelopePayload> DeepClone() =>
            new GenericEventEnvelope(AggregateType, Subject, Type, Data, Time);
    }

    protected static object GetGenericRandomPayload() => new {
        MyString = TestHelper.RandomId(),
        MyNumber = TestHelper.RandomId<long>(),
        MyNull = (string?)null!,
        MyBoolean = true
    };

    protected override EventEnvelopeBase<IEventEnvelopePayload> Sut(
        string? aggregateId,
        string? typeAggregate,
        string? typeEvent)
    {
        return new GenericEventEnvelope(typeAggregate!, aggregateId!, typeEvent!, GetGenericRandomPayload());
    }
}
