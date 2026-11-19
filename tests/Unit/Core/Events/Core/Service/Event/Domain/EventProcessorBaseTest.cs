using Anarchy.Toolkit.Core.Events.Service.Event.Domain;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Mock;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Service.Event.Domain;

using Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;

public class EventProcessorBaseTest
{
    private readonly Mock<EventHandlerBus<DummyStateEntity>> _mockEventHandlerBus;
    private readonly Mock<IAppTelemetry> _telemetry = new();
    private readonly TestEventProcessor _sut;

    public EventProcessorBaseTest()
    {
        _mockEventHandlerBus = EventHandlerBusMocker.Mock(true, new DummyStateEntityFixture().Build());
        _telemetry.Setup(t => t.BeginScope(It.IsAny<IReadOnlyCollection<KeyValuePair<string, object?>>>()))
            .Returns(Moq.Mock.Of<IDisposable>());
        _sut = new TestEventProcessor(_mockEventHandlerBus.Object, _telemetry.Object);
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
        _telemetry.Verify(t => t.BeginScope(It.Is<IReadOnlyCollection<KeyValuePair<string, object?>>>(properties =>
            properties.Any(property => property.Key == "AggregateId" && Equals(property.Value, fixtureEvents[0].AggregateId)) &&
            properties.Any(property => property.Key == "TypeEvent" && Equals(property.Value, fixtureEvents[0].TypeEvent)))), Times.Once);
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
        _telemetry.Verify(t => t.Log(LogLevel.Warning, It.IsAny<CallSite>(), null,
            "No states were altered after processing event", It.IsAny<object?[]>()), Times.Once);
        _telemetry.Verify(t => t.IncrementCounter(It.Is<string>(name => name.EndsWith(".empty_state_after_handler_bus")), 1,
            It.Is<KeyValuePair<string, object?>[]>(tags => tags.Any(tag => tag.Key == "success" && Equals(tag.Value, false)))), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOptionalHandlerHasNoState_ShouldSkipStoringState()
    {
        // Arrange: an optional handler without state on a bus processing an aggregate with no hydrated state
        var handler = new Handler.OptionalMutationDummyEventHandlerTest.OptionalMutationDummyEventHandler();
        var bus = new EventHandlerBus<DummyStateEntity>([handler]);
        var sut = new TestEventProcessor(bus, _telemetry.Object, hydrate: false);

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
        IAppTelemetry telemetry,
        bool hydrate = true
    )
        : EventProcessorBase<DummyStateEntity>(eventHandlerBus, telemetry)
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
