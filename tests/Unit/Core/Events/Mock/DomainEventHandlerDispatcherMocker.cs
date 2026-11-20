using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Domain;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Mock;

public static class DomainEventHandlerDispatcherMocker
{
    public static Moq.Mock<DomainEventHandlerDispatcher<T>> Mock<T>(bool canHandle, T fixtureModel)
        where T : IEntity
    {
        var mockHandler = DomainEventHandlerMocker.Mock(canHandle, fixtureModel);
        var fixtureHandlerCollection = ImmutableList.Create(mockHandler.Object);

        return new Moq.Mock<DomainEventHandlerDispatcher<T>>(fixtureHandlerCollection) { CallBase = true };
    }
}
