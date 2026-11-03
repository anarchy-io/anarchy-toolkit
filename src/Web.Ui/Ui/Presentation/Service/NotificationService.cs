using Anarchy.Toolkit.Web.Ui.Infrastructure.Service.Http.Client.Exceptions;

namespace Anarchy.Toolkit.Web.Ui.Ui.Presentation.Service;

public enum NotificationLevel
{
    Info,
    Success,
    Warning,
    Error
}

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
public sealed class NotificationService : IDisposable
{
    public static readonly TimeSpan AutoDismissDelay = TimeSpan.FromSeconds(10);

    // Must match the `notification-fade-out` animation duration in toolkit-ui.css.
    public static readonly TimeSpan FadeOutDuration = TimeSpan.FromMilliseconds(400);

    private readonly List<Notification> _notifications = [];
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _disposeCts = new();

    public event Action? Changed;

    public IReadOnlyList<Notification> Notifications {
        get {
            lock (_lock)
            {
                return _notifications.ToArray();
            }
        }
    }

    public void Info(string message, string? details = null) => Notify(NotificationLevel.Info, message, details);

    public void Success(string message, string? details = null) => Notify(NotificationLevel.Success, message, details);

    public void Warning(string message, string? details = null) => Notify(NotificationLevel.Warning, message, details);

    public void Error(string message, string? details = null) => Notify(NotificationLevel.Error, message, details);

    /// <summary>
    /// Reports an exception as an error. A failed downstream call shows its response body as details;
    /// any other exception shows its inner exception, when present.
    /// </summary>
    public void Error(Exception exception)
    {
        var details = exception is DownstreamHttpRequestException downstreamException
            ? downstreamException.ResponseContent
            : exception.InnerException?.ToString();

        Error(exception.Message, details);
    }

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

    public void Dispose()
    {
        _disposeCts.Cancel();
        _disposeCts.Dispose();
    }
}
