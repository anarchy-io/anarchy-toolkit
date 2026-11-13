namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

public class Disposable : IDisposable
{
    public static IDisposable Create(Action onDispose) => new Disposable(onDispose);

    public static IDisposable Empty { get; } = new Disposable(() => { });

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
