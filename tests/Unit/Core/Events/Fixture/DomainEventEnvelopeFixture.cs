using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Extensions.System;
using Anarchy.Toolkit.Testing.Fixture;
using Anarchy.Toolkit.Testing.Helpers.Tests;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;

public class DomainEventEnvelopeFixture : EntityFixtureBase<DomainEventEnvelope>
{
    protected override DomainEventEnvelope BuildDefaultFixture()
    {
        return DomainEventEnvelope.Create(
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

    public static DomainEventEnvelope CreateForReadyOrder(string? aggregateId = null)
    {
        return new DomainEventEnvelopeFixture().Build(new {
            AggregateType = DummyTypeAggregate.DummyOrder.ToDescription(),
            Subject = aggregateId ?? TestHelper.RandomId("Test.AggregateId"),
            Type = DummyTypeEvent.DummyOrderReady.ToDescription()
        });
    }
}
