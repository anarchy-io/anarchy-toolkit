using Anarchy.Toolkit.Core.Events.Domain;
using Anarchy.Toolkit.Core.Events.Domain.Exceptions;
using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.System;
using Anarchy.Toolkit.Testing.Telemetry;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Mock;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Domain;

public sealed class DomainEventProcessorBaseTest : IDisposable
{
    private readonly Mock<DomainEventHandlerDispatcher<DummyStateEntity>> _mockDomainEventHandlerDispatcher;
    private readonly TestTelemetry _telemetry = new();
    private readonly TestEventProcessor _sut;

    public DomainEventProcessorBaseTest()
    {
        _mockDomainEventHandlerDispatcher = DomainEventHandlerDispatcherMocker.Mock(true, new DummyStateEntityFixture().Build());
        _sut = new TestEventProcessor(_mockDomainEventHandlerDispatcher.Object, _telemetry.Telemetry);
    }

    public void Dispose() => _telemetry.Dispose();

    [Fact]
    public async Task ExecuteAsync_ShouldProcessEventsAndStoreStateEntities()
    {
        // Arrange
        var fixtureEvents = ImmutableArray.Create(DomainEventEnvelopeFixture.CreateForReadyOrder());

        // Act
        var result = await _sut.ExecuteAsync(fixtureEvents);

        // Assert
        Assert.Single(result);
        _mockDomainEventHandlerDispatcher.Verify(bus => bus.ProcessAsync(It.IsAny<DomainEventEnvelope>(),
            It.IsAny<DummyStateEntity>(), It.IsAny<CancellationToken?>()), Times.Once);
        _sut.StoredEntities.Should().HaveCount(1);
        var stored = _telemetry.Logs.Should().ContainSingle(log => log.Message.Contains("Storing state")).Which;
        stored.Scopes.Should().Contain("AggregateId", fixtureEvents[0].Subject)
            .And.Contain("TypeEvent", fixtureEvents[0].Type);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBusThrowsEmptyStateAfterHandlerDispatchException_ShouldSkipStoringState()
    {
        // Arrange
        _mockDomainEventHandlerDispatcher.Setup(bus => bus.ProcessAsync(It.IsAny<DomainEventEnvelope>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new EmptyStateAfterHandlerDispatchException());

        var fixtureEvents = ImmutableArray.Create(DomainEventEnvelopeFixture.CreateForReadyOrder());

        // Act
        var result = await _sut.ExecuteAsync(fixtureEvents);

        // Assert
        result.Should().HaveCount(1, "the hydrated state is returned untouched");
        _sut.StoredEntities.Should().BeEmpty();
        _telemetry.Logs.Should().ContainSingle(log => log.Level == LogLevel.Warning
                                                      && log.Message.Contains("No states were altered after processing event"));
        _telemetry.Metrics.Metrics.Should().ContainSingle(metric => metric.Name.EndsWith(".empty_state_after_handler_bus"))
            .Which.HasTag("success", false).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenOptionalHandlerHasNoState_ShouldSkipStoringState()
    {
        // Arrange: an optional handler without state on a bus processing an aggregate with no hydrated state
        var handler = new Handler.OptionalMutationDummyEventHandlerTest.OptionalMutationDummyEventHandler();
        var bus = new DomainEventHandlerDispatcher<DummyStateEntity>([handler]);
        var sut = new TestEventProcessor(bus, _telemetry.Telemetry, hydrate: false);

        var fixtureEvents = ImmutableArray.Create(new DomainEventEnvelope(
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
        _mockDomainEventHandlerDispatcher.Setup(bus => bus.ProcessAsync(It.IsAny<DomainEventEnvelope>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new NoHandlersForEventException("dummy"));

        var fixtureEvents = ImmutableArray.Create(DomainEventEnvelopeFixture.CreateForReadyOrder());

        // Act
        var act = async () => await _sut.ExecuteAsync(fixtureEvents);

        // Assert
        await act.Should().ThrowAsync<NoHandlersForEventException>();
        _sut.StoredEntities.Should().BeEmpty();
    }

    private class TestEventProcessor(
        DomainEventHandlerDispatcher<DummyStateEntity> domainEventHandlerDispatcher,
        AppTelemetry telemetry,
        bool hydrate = true
    )
        : DomainEventProcessorBase<DummyStateEntity>(domainEventHandlerDispatcher, telemetry)
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
            Telemetry.LogInformation(this.CalledFrom(), "Storing state");
            StoredEntities.Add(updatedStateEntity);
            return Task.FromResult(updatedStateEntity);
        }
    }
}
