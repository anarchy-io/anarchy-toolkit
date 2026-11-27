using Anarchy.Toolkit.Core.Events.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Core.Events.Local;

/// <summary>
/// In-process event bus abstraction.
/// </summary>
/// <remarks>
/// Register the default implementation with
/// <see cref="LocalEventServiceCollectionExtensions.AddLocalEventBus"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// await localEventBus.PublishAsync(new UserRegistered(userId), absorbErrors: false, ct);
/// </code>
/// </example>
public interface ILocalEventBus
{
    /// <summary>
    /// Publishes <paramref name="event"/> to every registered <see cref="ILocalEventHandler{TEvent}"/> for
    /// <typeparamref name="TEvent"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type; handlers are resolved for this exact type.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="absorbErrors">
    /// When <see langword="true"/> (default), handler failures are logged and the remaining handlers still run; when
    /// <see langword="false"/>, the first failure is rethrown.
    /// </param>
    /// <param name="ct">Cancellation token forwarded to the handlers.</param>
    /// <returns>A task that completes when every handler has run.</returns>
    Task PublishAsync<TEvent>(TEvent @event, bool absorbErrors = true, CancellationToken ct = default)
        where TEvent : ILocalEvent;
}

/// <summary>
/// Default DI-based implementation of <see cref="ILocalEventBus"/>.
/// </summary>
/// <remarks>
/// A new DI scope is created for each publish, so handlers may be registered as scoped or transient. Handlers run
/// sequentially, in registration order, awaiting each one. Publishing with no registered handlers is a no-op.
/// </remarks>
/// <param name="serviceProvider">The root service provider used to create a scope per publish.</param>
/// <param name="telemetry">The application telemetry used to log handler failures.</param>
public sealed class LocalEventBus(IServiceProvider serviceProvider, AppTelemetry telemetry) : ILocalEventBus
{
    /// <summary>
    /// Publishes <paramref name="event"/> to every registered <see cref="ILocalEventHandler{TEvent}"/> for
    /// <typeparamref name="TEvent"/>, sequentially and in registration order.
    /// </summary>
    /// <typeparam name="TEvent">The event type; handlers are resolved for this exact type.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="absorbErrors">
    /// When <see langword="true"/> (default), handler failures are logged and the remaining handlers still run; when
    /// <see langword="false"/>, the first failure is rethrown.
    /// </param>
    /// <param name="cancellationToken">Cancellation token forwarded to the handlers.</param>
    /// <returns>A task that completes when every handler has run.</returns>
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
                telemetry.Log(LogLevel.Error, caller, ex, "Failed to process in-memory event");

                if (!absorbErrors) throw;
            }
        }
    }
}
