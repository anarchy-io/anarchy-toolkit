using Anarchy.Toolkit.Core.Events.Service.Event.Domain;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain.Exceptions;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Dummy;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Fixture;
using Anarchy.Toolkit.Tests.Unit.Core.Events.Mock;
using AwesomeAssertions;
using Moq;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Core.Service.Event.Domain;

using Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;

public class EventHandlerBusTest : TestCaseBase
{
    private readonly Mock<IDomainEventHandler<DummyStateEntity>> _mockEventHandlerCanHandle;
    private readonly Mock<IDomainEventHandler<DummyStateEntity>> _mockEventHandlerCannotHandle;
    private readonly EventHandlerBus<DummyStateEntity> _sut;

    private readonly DummyStateEntity _fixtureGenericDomainModel = new DummyStateEntityFixture().Build();

    public EventHandlerBusTest()
    {
        _mockEventHandlerCannotHandle = DomainEventHandlerMocker.Mock<DummyStateEntity>(false);
        _mockEventHandlerCanHandle = DomainEventHandlerMocker.Mock(true, _fixtureGenericDomainModel);

        _sut = new EventHandlerBus<DummyStateEntity>(ImmutableList.Create(_mockEventHandlerCanHandle.Object,
            _mockEventHandlerCannotHandle.Object));
    }

    [Fact]
    public async Task ProcessAsync_WhenItIsNotValid_ShouldSkipHandler()
    {
        // Act
        await _sut.ProcessAsync(EventFixture.CreateForReadyOrder(), _fixtureGenericDomainModel);

        // Assert
        _mockEventHandlerCanHandle.Verify(
            x => x.HandleAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()),
            Times.Once);

        _mockEventHandlerCannotHandle.Verify(
            x => x.HandleAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenHandlerCheckFails_ShouldThrowException()
    {
        // Arrange
        _mockEventHandlerCanHandle.Setup(x =>
                x.CanHandleEvent(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>()))
            .Throws(new Exception());

        var act = async () => await _sut.ProcessAsync(EventFixture.CreateForReadyOrder(),
            _fixtureGenericDomainModel);

        // Act & Assert
        await act.Should().ThrowAsync<UnableToHandleEventException>();

        _mockEventHandlerCanHandle.Verify(
            x => x.CanHandleEvent(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>()),
            Times.Once);

        _mockEventHandlerCanHandle.Verify(
            x => x.HandleAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenHandlerProcessFails_ShouldThrowException()
    {
        // Arrange
        _mockEventHandlerCanHandle.Setup(x =>
                x.HandleAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()))
            .Throws(new Exception());

        var act = async () => await _sut.ProcessAsync(EventFixture.CreateForReadyOrder(),
            _fixtureGenericDomainModel);

        // Act & Assert
        await act.Should().ThrowAsync<UnableToHandleEventException>();

        _mockEventHandlerCanHandle.Verify(
            x => x.CanHandleEvent(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>()),
            Times.Once);

        _mockEventHandlerCanHandle.Verify(
            x => x.HandleAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenAllHandlersWork_ShouldFinish()
    {
        // Act
        var result = await _sut.ProcessAsync(EventFixture.CreateForReadyOrder(),
            _fixtureGenericDomainModel);

        // Assert
        result.Should().BeSameAs(_fixtureGenericDomainModel);

        _mockEventHandlerCanHandle.Verify(
            x => x.CanHandleEvent(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>()),
            Times.Once);

        _mockEventHandlerCanHandle.Verify(
            x => x.HandleAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenNoHandlersSupportIncomingEvent_ShouldThrowException()
    {
        // Arrange
        _mockEventHandlerCanHandle.Setup(x =>
                x.CanHandleEvent(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>()))
            .Returns(false);

        // Act
        var act = async () => await _sut.ProcessAsync(EventFixture.CreateForReadyOrder(),
            _fixtureGenericDomainModel);

        // Act & Assert
        await act.Should().ThrowAsync<NoHandlersForEventException>();
    }

    [Fact]
    public async Task ProcessAsync_WhenNoHandlersInitializeEmptyState_ShouldThrowException()
    {
        // Arrange
        _mockEventHandlerCanHandle.Setup(x =>
            x.HandleAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()));

        // Act
        var act = async () => await _sut.ProcessAsync(EventFixture.CreateForReadyOrder(), null);

        // Act & Assert
        await act.Should().ThrowAsync<EmptyStateAfterHandlerBusException>();
    }

    [Fact]
    public async Task ProcessAsync_WhenHandlerThrowsNoStateFoundForOptionalEventHandlerException_ShouldMarkAsHandledAndKeepExistingState()
    {
        // Arrange
        _mockEventHandlerCanHandle.Setup(x =>
                x.HandleAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new NoStateFoundForOptionalEventHandlerException());

        // Act
        var result = await _sut.ProcessAsync(EventFixture.CreateForReadyOrder(), _fixtureGenericDomainModel);

        // Assert
        result.Should().BeSameAs(_fixtureGenericDomainModel);

        _mockEventHandlerCanHandle.Verify(
            x => x.HandleAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenOnlyHandlerThrowsNoStateFoundForOptionalEventHandlerException_ShouldNotThrowNoHandlersForEventException()
    {
        // Arrange
        _mockEventHandlerCanHandle.Setup(x =>
                x.HandleAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new NoStateFoundForOptionalEventHandlerException());

        _mockEventHandlerCannotHandle.Setup(x =>
                x.CanHandleEvent(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>()))
            .Returns(false);

        // Act
        var act = async () => await _sut.ProcessAsync(EventFixture.CreateForReadyOrder(),
            _fixtureGenericDomainModel);

        // Act & Assert
        await act.Should().NotThrowAsync<NoHandlersForEventException>();
    }

    [Fact]
    public async Task ProcessAsync_WhenHandlerThrowsNoStateFoundForOptionalEventHandlerException_AndStateIsNull_ShouldThrowEmptyStateAfterHandlerBusException()
    {
        // Arrange
        _mockEventHandlerCanHandle.Setup(x =>
                x.HandleAsync(It.IsAny<Event>(), It.IsAny<DummyStateEntity?>(), It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new NoStateFoundForOptionalEventHandlerException());

        // Act
        var act = async () => await _sut.ProcessAsync(EventFixture.CreateForReadyOrder(), null);

        // Act & Assert
        await act.Should().ThrowAsync<EmptyStateAfterHandlerBusException>();
    }
}
