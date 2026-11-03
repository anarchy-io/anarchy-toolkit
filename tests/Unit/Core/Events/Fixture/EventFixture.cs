using Anarchy.Toolkit.Core.Events.Entity.Event.Domain;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Testing.Fixture;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;

public class EventFixture : EntityFixtureBase<Event>
{
    protected override Event BuildDefaultFixture()
    {
        return Event.Create(
            DummyTypeAggregate.DummyOrder,
            TestHelper.RandomId("Test.AggregateId"),
            DummyTypeEvent.DummyOrderReady,
            new DummyEventPayload {
                Status = TestHelper.RandomId("Test.Status"),
                Number = TestHelper.RandomId<long>(),
                LastModificationTime = TestHelper.LowPrecisionNow()
            },
            TestHelper.LowPrecisionNow());
    }

    public static Event CreateForReadyOrder(string? aggregateId = null)
    {
        return new EventFixture().Build(new {
            TypeAggregate = DummyTypeAggregate.DummyOrder.ToDescription(),
            AggregateId = aggregateId ?? TestHelper.RandomId("Test.AggregateId"),
            TypeEvent = DummyTypeEvent.DummyOrderReady.ToDescription()
        });
    }
}
