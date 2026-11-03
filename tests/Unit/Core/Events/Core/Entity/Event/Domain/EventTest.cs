using Anarchy.Toolkit.Core.Events.Entity.Event;
using Anarchy.Toolkit.Core.Events.Entity.Event.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using AwesomeAssertions;
using Newtonsoft.Json.Linq;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Entity.Event.Domain;

using Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;

public class EventTest() : GenericEventEnvelopeTest(
    DummyTypeAggregate.DummyOrder.ToDescription(),
    DummyTypeEvent.DummyOrderReady.ToDescription())
{
    protected override EventEnvelopeBase<IEventEnvelopePayload> Sut(
        string? aggregateId,
        string? typeAggregate,
        string? typeEvent)
    {
        return new Event(
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

        var sut = Event.Create(DummyTypeAggregate.DummyTask, aggregateId, DummyTypeEvent.DummyOrderCancelled);

        sut.TypeAggregate.Should().Be("dummy_task");
        sut.AggregateId.Should().Be(aggregateId);
        sut.TypeEvent.Should().Be("dummy_order_cancelled");
        sut.Payload.Should().BeNull();
    }

    [Fact]
    public void Constructor_TrimsIdentityValues()
    {
        var sut = new Event(" aggregate ", " id ", " event ");

        sut.TypeAggregate.Should().Be("aggregate");
        sut.AggregateId.Should().Be("id");
        sut.TypeEvent.Should().Be("event");
    }

    [Fact]
    public void Constructor_WhenMomentIsNotProvided_DefaultsToNow()
    {
        var before = DateTimeOffset.UtcNow;

        var sut = new Event("aggregate", "id", "event");

        sut.Moment.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Constructor_WhenPayloadIsJsonString_DeserializesPayload()
    {
        var sut = new Event("aggregate", "id", "event", "{\"Status\":\"ready\",\"Number\":42}");

        sut.Payload.Should().BeOfType<JObject>();
        sut.CastPayload<DummyEventPayload>().Should().BeEquivalentTo(new { Status = "ready", Number = 42L });
    }

    [Fact]
    public void Constructor_WhenPayloadIsInvalidJsonString_ThrowsException()
    {
        var act = () => new Event("aggregate", "id", "event", "{ not-json");

        act.Should().Throw<EnvelopePayloadDeserializationException>();
    }

    [Fact]
    public void CastPayload_WhenPayloadIsTypedObject_ReturnsSameInstance()
    {
        var payload = new DummyEventPayload { Status = "ready" };
        var sut = new Event("aggregate", "id", "event", payload);

        sut.CastPayload<DummyEventPayload>().Should().BeSameAs(payload);
    }

    [Fact]
    public void CastPayload_WhenPayloadIsNull_ThrowsException()
    {
        var sut = new Event("aggregate", "id", "event");

        var act = () => sut.CastPayload<DummyEventPayload>();

        act.Should().Throw<EmptyEnvelopePayloadParseException<DummyEventPayload>>();
    }

    [Fact]
    public void CastPayload_WhenPayloadIsIncompatible_ThrowsException()
    {
        var sut = new Event("aggregate", "id", "event", new DummyEventPayload());

        var act = () => sut.CastPayload<DummyStateEntity>();

        act.Should().Throw<InvalidEnvelopePayloadParseException<DummyStateEntity>>();
    }
}
