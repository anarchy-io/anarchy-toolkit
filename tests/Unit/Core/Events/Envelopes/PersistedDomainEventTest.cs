using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using AwesomeAssertions;

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
        sut.TypeAggregate.Should().Be("dummy_order");
        sut.TypeEvent.Should().Be("dummy_order_created");
    }

    [Fact]
    public void DeepClone_KeepsIdAndMoment()
    {
        var sut = PersistedDomainEvent.Create(42L, DummyTypeAggregate.DummyOrder, "id", DummyTypeEvent.DummyOrderCreated);

        var clone = sut.DeepClone();

        clone.Should().NotBeSameAs(sut);
        clone.Id.Should().Be(42L);
        clone.Moment.Should().Be(sut.Moment);
    }
}
