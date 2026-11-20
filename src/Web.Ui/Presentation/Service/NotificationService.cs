using Anarchy.Toolkit.Web.Ui.Infrastructure.Http.Client.Exceptions;

namespace Anarchy.Toolkit.Web.Ui.Presentation.Service;

/// <summary>Severity of a <see cref="Notification"/>, used to style it.</summary>
public enum NotificationLevel
{
    /// <summary>Informational message.</summary>
    Info,
    /// <summary>A successful operation.</summary>
    Success,
    /// <summary>A warning.</summary>
    Warning,
    /// <summary>An error.</summary>
    Error
}

/// <summary>A floating notification managed by <see cref="NotificationService"/>.</summary>
/// <param name="Id">Unique identifier, used to dismiss the notification.</param>
/// <param name="Level">The severity.</param>
/// <param name="Message">The message shown to the user.</param>
/// <param name="Details">Optional details (e.g. a response body or inner exception), or <see langword="null"/>.</param>
/// <param name="IsDismissing">Whether the notification is fading out and about to be removed.</param>
public sealed record Notification(
    Guid Id,
    NotificationLevel Level,
    string Message,
    string? Details,
    bool IsDismissing = false);

/// <summary>
/// Circuit-scoped store of floating notifications. Notifications stack (newest first) and are
/// dismissed either by the user or automatically after <see cref="AutoDismissDelay"/>. Dismissal
/// first flags the notification as "dismissing" (so the UI can fade it out) and removes it after
/// <see cref="FadeOutDuration"/>.
/// </summary>
/// <remarks>
/// Registered as a scoped service by <c>AddToolkitUiPresentation</c> and rendered by the <c>NotificationHost</c>
/// component, which re-renders on <see cref="Changed"/>. The list is guarded by a lock, so notifications can be
/// raised from background tasks; <see cref="Changed"/> may then fire off the renderer's synchronization context.
/// Disposing cancels pending automatic dismissals.
/// </remarks>
/// <example>
/// <code language="csharp">
/// try
/// {
///     await client.SaveAsync(order);
///     notifications.Success("Order saved.");
/// }
/// catch (Exception exception)
/// {
///     notifications.Error(exception);
/// }
/// </code>
/// </example>
public sealed class NotificationService : IDisposable
{
    /// <summary>How long a notification stays visible before it is dismissed automatically (10 seconds).</summary>
    public static readonly TimeSpan AutoDismissDelay = TimeSpan.FromSeconds(10);

    // Must match the `notification-fade-out` animation duration in css/toolkit-ui.css.
    /// <summary>How long a dismissing notification is kept so the UI can fade it out (400 ms).</summary>
    public static readonly TimeSpan FadeOutDuration = TimeSpan.FromMilliseconds(400);

    private readonly List<Notification> _notifications = [];
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _disposeCts = new();

    /// <summary>Raised whenever a notification is added, starts dismissing or is removed.</summary>
    public event Action? Changed;

    /// <summary>A snapshot of the current notifications, newest first.</summary>
    public IReadOnlyList<Notification> Notifications {
        get {
            lock (_lock)
            {
                return _notifications.ToArray();
            }
        }
    }

    /// <summary>Shows an <see cref="NotificationLevel.Info"/> notification.</summary>
    /// <param name="message">The message; blank messages are ignored.</param>
    /// <param name="details">Optional details.</param>
    public void Info(string message, string? details = null) => Notify(NotificationLevel.Info, message, details);

    /// <summary>Shows a <see cref="NotificationLevel.Success"/> notification.</summary>
    /// <param name="message">The message; blank messages are ignored.</param>
    /// <param name="details">Optional details.</param>
    public void Success(string message, string? details = null) => Notify(NotificationLevel.Success, message, details);

    /// <summary>Shows a <see cref="NotificationLevel.Warning"/> notification.</summary>
    /// <param name="message">The message; blank messages are ignored.</param>
    /// <param name="details">Optional details.</param>
    public void Warning(string message, string? details = null) => Notify(NotificationLevel.Warning, message, details);

    /// <summary>Shows an <see cref="NotificationLevel.Error"/> notification.</summary>
    /// <param name="message">The message; blank messages are ignored.</param>
    /// <param name="details">Optional details.</param>
    public void Error(string message, string? details = null) => Notify(NotificationLevel.Error, message, details);

    /// <summary>
    /// Reports an exception as an error. A failed downstream call shows its response body as details;
    /// any other exception shows its inner exception, when present.
    /// </summary>
    /// <param name="exception">The exception to report; its message is used as the notification message.</param>
    public void Error(Exception exception)
    {
        var details = exception is DownstreamHttpRequestException downstreamException
            ? downstreamException.ResponseContent
            : exception.InnerException?.ToString();

        Error(exception.Message, details);
    }

    /// <summary>
    /// Adds a notification at the top of the stack and schedules its automatic dismissal. Blank messages are ignored
    /// and blank details are dropped.
    /// </summary>
    /// <param name="level">The severity.</param>
    /// <param name="message">The message.</param>
    /// <param name="details">Optional details.</param>
    public void Notify(NotificationLevel level, string message, string? details = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var notification = new Notification(Guid.NewGuid(), level, message,
            string.IsNullOrWhiteSpace(details) ? null : details);

        lock (_lock)
        {
            _notifications.Insert(0, notification);
        }

        Changed?.Invoke();

        _ = AutoDismissAsync(notification.Id);
    }

    /// <summary>
    /// Starts dismissing the notification (fade-out, then removal). Unknown or already dismissing notifications are ignored.
    /// </summary>
    /// <param name="id">The notification <see cref="Notification.Id"/>.</param>
    public void Dismiss(Guid id) => _ = DismissAsync(id);

    private async Task AutoDismissAsync(Guid id)
    {
        try
        {
            await Task.Delay(AutoDismissDelay, _disposeCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await DismissAsync(id);
    }

    private async Task DismissAsync(Guid id)
    {
        lock (_lock)
        {
            var index = _notifications.FindIndex(n => n.Id == id);
            if (index < 0 || _notifications[index].IsDismissing)
            {
                return;
            }

            _notifications[index] = _notifications[index] with { IsDismissing = true };
        }

        Changed?.Invoke();

        try
        {
            await Task.Delay(FadeOutDuration, _disposeCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        lock (_lock)
        {
            _notifications.RemoveAll(n => n.Id == id);
        }

        Changed?.Invoke();
    }

    /// <summary>Cancels pending automatic dismissals and fade-outs.</summary>
    public void Dispose()
    {
        _disposeCts.Cancel();
        _disposeCts.Dispose();
    }
}
