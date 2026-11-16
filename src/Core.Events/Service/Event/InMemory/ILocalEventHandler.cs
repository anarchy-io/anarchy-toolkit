using Anarchy.Toolkit.Core.Events.Entity.Event.InMemory;

namespace Anarchy.Toolkit.Core.Events.Service.Event.InMemory;

/// <summary>
/// Handler for in-process events.
/// </summary>
/// <remarks>
/// Register implementations in the DI container; <see cref="LocalEventBus"/> resolves every handler for the published
/// event type from a fresh scope on each publish.
/// </remarks>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <example>
/// <code language="csharp">
/// public sealed class SendWelcomeEmailHandler(IEmailSender sender) : ILocalEventHandler&lt;UserRegistered&gt;
/// {
///     public Task HandleAsync(UserRegistered @event, CancellationToken? cancellationToken = null)
///         =&gt; sender.SendWelcomeAsync(@event.UserId);
/// }
/// </code>
/// </example>
public interface ILocalEventHandler<in TEvent>
    where TEvent : ILocalEvent
{
    /// <summary>
    /// Handles a published event.
    /// </summary>
    /// <param name="event">The published event.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes when the event has been handled.</returns>
    Task HandleAsync(TEvent @event, CancellationToken? cancellationToken = null);
}
