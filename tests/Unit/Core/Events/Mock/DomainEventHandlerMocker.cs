using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Entity.Event.Domain;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Mock;

public static class DomainEventHandlerMocker
{
    public static Mock<IDomainEventHandler<T>> Mock<T>(bool canHandle, T? fixtureResultModel = default)
        where T : IEntity
    {
        var mockResult = new Mock<IDomainEventHandler<T>>();
        mockResult.Setup(x => x.CanHandleEvent(It.IsAny<Event>(), It.IsAny<T?>()))
            .Returns(canHandle);

        if (canHandle && fixtureResultModel is not null)
            mockResult.Setup(x => x.HandleAsync(It.IsAny<Event>(), It.IsAny<T?>(), It.IsAny<CancellationToken?>()))
                .ReturnsAsync(fixtureResultModel);

        return mockResult;
    }
}
