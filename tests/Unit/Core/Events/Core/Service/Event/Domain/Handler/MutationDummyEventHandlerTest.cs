using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Service.Event.Domain.Handler;

using Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;

public class MutationDummyEventHandlerTest : DomainEventHandlerTestBase<DummyStateEntity>
{
    internal class MutationDummyEventHandler()
        : StateMutationDomainEventHandlerBase<DummyStateEntity>(new() {
            [DummyTypeAggregate.Unknown.ToDescription()] = [TestHelper.RandomEnum([DummyTypeEvent.Unknown]).ToDescription()]
        })
    {
        protected override Task<DummyStateEntity> HandleAsync(DummyStateEntity modelEntity,
                                                              Event _,
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
