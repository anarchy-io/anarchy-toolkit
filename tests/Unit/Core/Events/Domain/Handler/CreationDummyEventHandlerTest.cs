using Anarchy.Toolkit.Core.Events.Domain.Handler;
using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Domain.Handler;

public class CreationDummyEventHandlerTest : DomainEventHandlerTestBase<DummyStateEntity>
{
    internal class CreationDummyEventHandler()
        : StateCreationDomainEventHandlerBase<DummyStateEntity>(
            Supports(DummyTypeAggregate.Unknown, TestHelper.RandomEnum([DummyTypeEvent.Unknown])))
    {
        public override Task<DummyStateEntity> HandleAsync(DomainEventEnvelope domainEvent,
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
