using Anarchy.Toolkit.Core.Events.Entity.Event.InMemory;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Core.Events.Service.Event.InMemory;

/// <summary>
/// In-process event bus abstraction.
/// </summary>
public interface ILocalEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, bool absorbErrors = true, CancellationToken ct = default)
        where TEvent : ILocalEvent;
}

/// <summary>
/// Default DI-based implementation of <see cref="ILocalEventBus"/>.
/// </summary>
public sealed class LocalEventBus(IServiceProvider serviceProvider, Observer observer) : ILocalEventBus
{
    public async Task PublishAsync<TEvent>(TEvent @event, bool absorbErrors = true,
                                                                  CancellationToken cancellationToken = default)
        where TEvent : ILocalEvent
    {
        var caller = this.CalledFrom();

        // Resolve handlers per publish so they can be scoped/transient
        using var scope = serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<ILocalEventHandler<TEvent>>().ToArray();

        if (handlers.Length == 0)
        {
            return;
        }

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(@event, cancellationToken);
            }
            catch (Exception ex)
            {
                observer.LogError(ex, caller, "Failed to process in-memory event");

                if (!absorbErrors) throw;
            }
        }
    }
}
