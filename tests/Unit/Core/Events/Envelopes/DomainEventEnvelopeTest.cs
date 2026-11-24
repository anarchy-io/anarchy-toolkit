using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Events.Envelopes.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using AwesomeAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Envelopes;

public class DomainEventEnvelopeTest() : GenericEventEnvelopeTest(
    DummyTypeAggregate.DummyOrder.ToDescription(),
    DummyTypeEvent.DummyOrderReady.ToDescription())
{
    protected override EventEnvelopeBase<IEventEnvelopePayload> Sut(
        string? aggregateId,
        string? typeAggregate,
        string? typeEvent)
    {
        return new DomainEventEnvelope(
            typeAggregate!,
            aggregateId!,
            typeEvent!,
            GetGenericRandomPayload(),
            TestHelper.LowPrecisionNow());
    }

    [Fact]
    public void Create_WithEnums_ResolvesDescriptions()
    {
        var aggregateId = TestHelper.RandomId();

        var sut = DomainEventEnvelope.Create(DummyTypeAggregate.DummyTask, aggregateId, DummyTypeEvent.DummyOrderCancelled);

        sut.AggregateType.Should().Be("dummy_task");
        sut.Subject.Should().Be(aggregateId);
        sut.Type.Should().Be("dummy_order_cancelled");
        sut.Data.Should().BeNull();
        sut.CustomAttributes.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_TrimsIdentityValues()
    {
        var sut = new DomainEventEnvelope(" aggregate ", " id ", " event ");

        sut.AggregateType.Should().Be("aggregate");
        sut.Subject.Should().Be("id");
        sut.Type.Should().Be("event");
    }

    [Fact]
    public void Constructor_WhenTimeIsNotProvided_DefaultsToNow()
    {
        var before = DateTimeOffset.UtcNow;

        var sut = new DomainEventEnvelope("aggregate", "id", "event");

        sut.Time.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Constructor_WhenDataIsJsonString_DeserializesData()
    {
        var sut = new DomainEventEnvelope("aggregate", "id", "event", "{\"Status\":\"ready\",\"Number\":42}");

        sut.Data.Should().BeOfType<JObject>();
        sut.CastData<DummyEventPayload>().Should().BeEquivalentTo(new { Status = "ready", Number = 42L });
    }

    [Fact]
    public void Constructor_WhenDataIsInvalidJsonString_ThrowsException()
    {
        var act = () => new DomainEventEnvelope("aggregate", "id", "event", "{ not-json");

        act.Should().Throw<EnvelopePayloadDeserializationException>();
    }

    [Fact]
    public void CastData_WhenDataIsTypedObject_ReturnsSameInstance()
    {
        var payload = new DummyEventPayload { Status = "ready" };
        var sut = new DomainEventEnvelope("aggregate", "id", "event", payload);

        sut.CastData<DummyEventPayload>().Should().BeSameAs(payload);
    }

    [Fact]
    public void CastData_WhenDataIsNull_ThrowsException()
    {
        var sut = new DomainEventEnvelope("aggregate", "id", "event");

        var act = () => sut.CastData<DummyEventPayload>();

        act.Should().Throw<EmptyEnvelopePayloadParseException<DummyEventPayload>>();
    }

    [Fact]
    public void CastData_WhenDataIsIncompatible_ThrowsException()
    {
        var sut = new DomainEventEnvelope("aggregate", "id", "event", new DummyEventPayload());

        var act = () => sut.CastData<DummyStateEntity>();

        act.Should().Throw<InvalidEnvelopePayloadParseException<DummyStateEntity>>();
    }

    [Fact]
    public void CustomAttributes_AreSnapshottedAndPreservedByCloneAndJson()
    {
        var attributes = new Dictionary<string, string> { ["correlationid"] = "original" };
        var sut = DomainEventEnvelope.Create(DummyTypeAggregate.DummyOrder, "order-1", DummyTypeEvent.DummyOrderReady,
            data: "\"ready\"", customAttributes: attributes);
        attributes["correlationid"] = "modified";
        attributes["late"] = "ignored";

        var clone = sut.DeepClone();
        var json = JsonConvert.SerializeObject(sut);
        var roundTrip = JsonConvert.DeserializeObject<DomainEventEnvelope>(json);

        sut.CustomAttributes.Should().BeEquivalentTo(new Dictionary<string, string> { ["correlationid"] = "original" });
        clone.Should().BeEquivalentTo(sut, options => options.Excluding(x => x.Data));
        clone.CastData<string>().Should().Be("ready");
        roundTrip.Should().BeEquivalentTo(sut, options => options.Excluding(x => x.Data));
        roundTrip!.CastData<string>().Should().Be("ready");
    }
}
