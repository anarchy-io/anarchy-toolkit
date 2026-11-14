namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

/// <summary>
/// An <see cref="IDisposable"/> running an action on its first disposal only.
/// </summary>
public class Disposable : IDisposable
{
    /// <summary>
    /// A disposable running <paramref name="onDispose"/> once, when first disposed.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onDispose"/> is null.</exception>
    public static IDisposable Create(Action onDispose) => new Disposable(onDispose);

    /// <summary>
    /// A disposable that does nothing.
    /// </summary>
    public static IDisposable Empty { get; } = new Disposable(() => { });

    /// <summary>
    /// Runs the action, unless it already ran.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _onDispose();
        _isDisposed = true;
    }

    private Disposable(Action onDispose)
    {
        _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

    private bool _isDisposed;
    private readonly Action _onDispose;
}
