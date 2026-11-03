using Anarchy.Toolkit.Core.Events.Service.Event.Domain;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Mock;
using AwesomeAssertions;
using Moq;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Service.Event.Domain;

using Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;

public class EventProcessorBaseTest : TestCaseBase
{
    private readonly Mock<EventHandlerBus<DummyStateEntity>> _mockEventHandlerBus;
    private readonly TestEventProcessor _sut;

    public EventProcessorBaseTest()
    {
        _mockEventHandlerBus = EventHandlerBusMocker.Mock(true, new DummyStateEntityFixture().Build());
        _sut = new TestEventProcessor(_mockEventHandlerBus.Object, FixtureObserver);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessEventsAndStoreStateEntities()
    {
        // Arrange
        var fixtureEvents = ImmutableArray.Create(EventFixture.CreateForReadyOrder());

        // Act
        var result = await _sut.ExecuteAsync(fixtureEvents);

        // Assert
        Assert.Single(result);
        _mockEventHandlerBus.Verify(bus => bus.ProcessAsync(It.IsAny<Event>(),
            It.IsAny<DummyStateEntity>(), It.IsAny<CancellationToken?>()), Times.Once);
        _sut.StoredEntities.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBusThrowsEmptyStateAfterHandlerBusException_ShouldSkipStoringState()
    {
        // Arrange
        _mockEventHandlerBus.Setup(bus => bus.ProcessAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new EmptyStateAfterHandlerBusException());

        var fixtureEvents = ImmutableArray.Create(EventFixture.CreateForReadyOrder());

        // Act
        var result = await _sut.ExecuteAsync(fixtureEvents);

        // Assert
        result.Should().HaveCount(1, "the hydrated state is returned untouched");
        _sut.StoredEntities.Should().BeEmpty();
        FixtureObserver.SinkLogs.Messages.Should()
            .Contain(m => m.MessageTemplate.Text.Contains("No states were altered after processing event"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenOptionalHandlerHasNoState_ShouldSkipStoringState()
    {
        // Arrange: an optional handler without state on a bus processing an aggregate with no hydrated state
        var handler = new Handler.OptionalMutationDummyEventHandlerTest.OptionalMutationDummyEventHandler();
        var bus = new EventHandlerBus<DummyStateEntity>([handler]);
        var sut = new TestEventProcessor(bus, FixtureObserver, hydrate: false);

        var fixtureEvents = ImmutableArray.Create(new Event(
            Handler.OptionalMutationDummyEventHandlerTest.SupportedTypeAggregate,
            "missing-aggregate",
            Handler.OptionalMutationDummyEventHandlerTest.SupportedTypeEvent));

        // Act
        var result = await sut.ExecuteAsync(fixtureEvents);

        // Assert
        result.Should().BeEmpty();
        sut.StoredEntities.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenBusThrowsNoHandlersForEventException_ShouldRethrow()
    {
        // Arrange
        _mockEventHandlerBus.Setup(bus => bus.ProcessAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new NoHandlersForEventException("dummy"));

        var fixtureEvents = ImmutableArray.Create(EventFixture.CreateForReadyOrder());

        // Act
        var act = async () => await _sut.ExecuteAsync(fixtureEvents);

        // Assert
        await act.Should().ThrowAsync<NoHandlersForEventException>();
        _sut.StoredEntities.Should().BeEmpty();
    }

    private class TestEventProcessor(
        EventHandlerBus<DummyStateEntity> eventHandlerBus,
        Observer observer,
        bool hydrate = true
    )
        : EventProcessorBase<DummyStateEntity>(eventHandlerBus, observer)
    {
        public readonly List<DummyStateEntity> StoredEntities = [];

        protected override Task<Dictionary<string, DummyStateEntity>> HydrateStateEntitiesAsync(string[] entityIds,
            CancellationToken? cancellationToken = null)
        {
            var result = new Dictionary<string, DummyStateEntity>();

            if (!hydrate)
            {
                return Task.FromResult(result);
            }

            foreach (var entityId in entityIds)
            {
                result[entityId] = new DummyStateEntityFixture().Build(new { AggregateId = entityId });
            }

            return Task.FromResult(result);
        }

        protected override Task<DummyStateEntity> StoreStateEntity(DummyStateEntity updatedStateEntity,
            CancellationToken? cancellationToken = null)
        {
            StoredEntities.Add(updatedStateEntity);
            return Task.FromResult(updatedStateEntity);
        }
    }
}
