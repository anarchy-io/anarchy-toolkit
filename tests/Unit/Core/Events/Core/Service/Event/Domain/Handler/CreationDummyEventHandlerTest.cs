using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Service.Event.Domain.Handler;

using Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;

public class CreationDummyEventHandlerTest : DomainEventHandlerTestBase<DummyStateEntity>
{
    internal class CreationDummyEventHandler()
        : StateCreationDomainEventHandlerBase<DummyStateEntity>(
            Supports(DummyTypeAggregate.Unknown, TestHelper.RandomEnum([DummyTypeEvent.Unknown])))
    {
        public override Task<DummyStateEntity> HandleAsync(Event domainEvent,
                                                           DummyStateEntity? entity,
                                                           CancellationToken? cancellationToken = null)
        {
            return Task.FromResult(new DummyStateEntityFixture().Build());
        }
    }

    protected override DummyStateEntity FixtureModelEntity() =>
        new DummyStateEntityFixture().Build(new { AggregateId });

    public CreationDummyEventHandlerTest()
    {
        Sut = new CreationDummyEventHandler();
    }
}
