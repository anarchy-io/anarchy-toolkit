using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Events.Service.Event.Domain;
using System.Collections.Immutable;

namespace Anarchy.Toolkit.Tests.Unit.Core.Events.Mock;

public static class EventHandlerBusMocker
{
    public static Moq.Mock<EventHandlerBus<T>> Mock<T>(bool canHandle, T fixtureModel)
        where T : IEntity
    {
        var mockHandler = DomainEventHandlerMocker.Mock(canHandle, fixtureModel);
        var fixtureHandlerCollection = ImmutableList.Create(mockHandler.Object);

        return new Moq.Mock<EventHandlerBus<T>>(fixtureHandlerCollection) { CallBase = true };
    }
}
