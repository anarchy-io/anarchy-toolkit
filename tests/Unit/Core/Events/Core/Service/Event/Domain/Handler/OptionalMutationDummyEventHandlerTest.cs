using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Handler;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Service.Event.Domain.Handler;

using Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;

public class OptionalMutationDummyEventHandlerTest : DomainEventHandlerTestBase<DummyStateEntity>
{
    public static readonly string SupportedTypeAggregate = DummyTypeAggregate.DummyOrder.ToDescription();
    public static readonly string SupportedTypeEvent = DummyTypeEvent.DummyOrderCancelled.ToDescription();

    internal class OptionalMutationDummyEventHandler()
        : OptionalStateMutationDomainEventHandlerBase<DummyStateEntity>(
            Supports(DummyTypeAggregate.DummyOrder, DummyTypeEvent.DummyOrderCancelled))
    {
        protected override Task<DummyStateEntity> HandleAsync(DummyStateEntity modelEntity,
                                                              Event domainEvent,
                                                              CancellationToken? cancellationToken = null)
        {
            modelEntity.Status = domainEvent.TypeEvent;
            return Task.FromResult(modelEntity);
        }
    }

    protected override DummyStateEntity FixtureModelEntity() =>
        new DummyStateEntityFixture().Build(new { AggregateId });

    public OptionalMutationDummyEventHandlerTest()
    {
        Sut = new OptionalMutationDummyEventHandler();
    }

    [Fact]
    public async Task HandleAsync_WhenEntityIsPresent_MutatesState()
    {
        var entity = FixtureModelEntity();

        var result = await Sut.HandleAsync(FixtureEvent(), entity);

        result.Should().BeSameAs(entity);
        result.Status.Should().Be(SupportedTypeEvent);
    }
}
