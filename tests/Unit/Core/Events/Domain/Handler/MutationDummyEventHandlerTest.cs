using Anarchy.Toolkit.Core.Events.Domain.Handler;
using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Extensions.System;
using Anarchy.Toolkit.Testing.Helpers.Tests;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Domain.Handler;

public class MutationDummyEventHandlerTest : DomainEventHandlerTestBase<DummyStateEntity>
{
    internal class MutationDummyEventHandler()
        : StateMutationDomainEventHandlerBase<DummyStateEntity>(new() {
            [DummyTypeAggregate.Unknown.ToDescription()] = [TestHelper.RandomEnum([DummyTypeEvent.Unknown]).ToDescription()]
        })
    {
        protected override Task<DummyStateEntity> HandleAsync(DummyStateEntity modelEntity,
                                                              DomainEventEnvelope _,
                                                              CancellationToken? cancellationToken = null)
        {
            return Task.FromResult(modelEntity);
        }
    }

    protected override DummyStateEntity FixtureModelEntity() =>
        new DummyStateEntityFixture().Build(new { AggregateId });

    public MutationDummyEventHandlerTest()
    {
        Sut = new MutationDummyEventHandler();
    }
}
