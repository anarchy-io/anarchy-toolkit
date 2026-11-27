using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Extensions.System;
using Anarchy.Toolkit.Testing.Helpers.Tests;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using AwesomeAssertions;
using Newtonsoft.Json;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Envelopes;

public class PersistedDomainEventTest() : GenericEventEnvelopeTest(
    DummyTypeAggregate.DummyOrder.ToDescription(),
    DummyTypeEvent.DummyOrderCreated.ToDescription())
{
    protected override EventEnvelopeBase<IEventEnvelopePayload> Sut(
        string? aggregateId,
        string? typeAggregate,
        string? typeEvent)
    {
        return new PersistedDomainEvent(
            TestHelper.RandomId<long>(),
            typeAggregate!,
            aggregateId!,
            typeEvent!,
            GetGenericRandomPayload(),
            TestHelper.LowPrecisionNow());
    }

    [Fact]
    public void Create_WithEnums_ResolvesDescriptionsAndKeepsId()
    {
        var sut = PersistedDomainEvent.Create(42L, DummyTypeAggregate.DummyOrder, "id", DummyTypeEvent.DummyOrderCreated);

        sut.Id.Should().Be(42L);
        sut.AggregateType.Should().Be("dummy_order");
        sut.Type.Should().Be("dummy_order_created");
    }

    [Fact]
    public void DeepCloneAndJson_KeepStorageIdTimeAndCustomAttributes()
    {
        var sut = PersistedDomainEvent.Create(42L, DummyTypeAggregate.DummyOrder, "id", DummyTypeEvent.DummyOrderCreated,
            data: "[1,2]", customAttributes: new Dictionary<string, string> { ["origin"] = "orders" });

        var clone = sut.DeepClone();
        var json = JsonConvert.SerializeObject(sut);
        var roundTrip = JsonConvert.DeserializeObject<PersistedDomainEvent>(json);

        clone.Should().NotBeSameAs(sut);
        clone.Should().BeEquivalentTo(sut, options => options.Excluding(x => x.Data));
        clone.CastData<int[]>().Should().Equal(1, 2);
        roundTrip.Should().BeEquivalentTo(sut, options => options.Excluding(x => x.Data));
        roundTrip!.CastData<int[]>().Should().Equal(1, 2);
    }
}
