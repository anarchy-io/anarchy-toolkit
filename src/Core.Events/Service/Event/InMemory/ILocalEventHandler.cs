using Anarchy.Toolkit.Core.Events.Entity.Event.InMemory;

namespace Anarchy.Toolkit.Core.Events.Service.Event.InMemory;

/// <summary>
/// Handler for in-process events.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public interface ILocalEventHandler<in TEvent>
    where TEvent : ILocalEvent
{
    System.Threading.Tasks.Task HandleAsync(TEvent @event, CancellationToken? cancellationToken = null);
}
