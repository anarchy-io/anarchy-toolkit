using Anarchy.Toolkit.Core.Events.Domain.Handler;
using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Extensions.System;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Domain.Handler;

public class OptionalMutationDummyEventHandlerTest : DomainEventHandlerTestBase<DummyStateEntity>
{
    public static readonly string SupportedTypeAggregate = DummyTypeAggregate.DummyOrder.ToDescription();
    public static readonly string SupportedTypeEvent = DummyTypeEvent.DummyOrderCancelled.ToDescription();

    internal class OptionalMutationDummyEventHandler()
        : OptionalStateMutationDomainEventHandlerBase<DummyStateEntity>(
            Supports(DummyTypeAggregate.DummyOrder, DummyTypeEvent.DummyOrderCancelled))
    {
        protected override Task<DummyStateEntity> HandleAsync(DummyStateEntity modelEntity,
                                                              DomainEventEnvelope domainEvent,
                                                              CancellationToken? cancellationToken = null)
        {
            modelEntity.Status = domainEvent.Type;
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
