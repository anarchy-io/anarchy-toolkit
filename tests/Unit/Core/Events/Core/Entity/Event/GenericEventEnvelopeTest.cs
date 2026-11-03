using Anarchy.Toolkit.Core.Events.Entity.Event;
using Anarchy.Toolkit.Testing.Helper;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Entity.Event;

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
        object? payload = null)
        : EventEnvelopeBase<IEventEnvelopePayload>(typeAggregate, aggregateId, typeEvent, payload,
            TestHelper.LowPrecisionNow())
    {
        public override EventEnvelopeBase<IEventEnvelopePayload> DeepClone() =>
            new GenericEventEnvelope(TypeAggregate, AggregateId, TypeEvent, Payload);
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
