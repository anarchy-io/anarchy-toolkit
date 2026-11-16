using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;

/// <summary>
///     Uses the <c>AsyncLocal</c> to store values that are scoped to the logical async call chain, not just the thread.
/// </summary>
/// <remarks>
///     Values set inside <see cref="Run"/>/<see cref="RunAsync{TResult}"/> flow into child tasks but changes made by
///     children do not flow back to the caller. Register as a singleton: each async flow still sees its own value.
/// </remarks>
/// <typeparam name="TValue">The type of value saved</typeparam>
public class AsyncLocalFlowContext<TValue> : IAsyncFlowContext<TValue>
{
    private readonly AsyncLocal<TValue?> _current = new();

    /// <inheritdoc/>
    public TValue Value {
        get => _current.Value ?? throw new EmptyAsyncContextException();
    }

    /// <inheritdoc/>
    public bool HasValue {
        get => TryGetValue(out _);
    }

    /// <inheritdoc/>
    public bool TryGetValue(out TValue? value, TValue? defaultValue = default)
    {
        value = _current.Value ?? defaultValue;
        return _current.Value is not null;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _current.Value = default;
    }

    /// <inheritdoc/>
    public void Run(TValue value, Action action)
    {
        var original = _current.Value;
        _current.Value = value;
        try
        {
            action();
        }
        finally
        {
            _current.Value = original;
        }
    }

    /// <inheritdoc/>
    /// <remarks><paramref name="cancellationToken"/> is not observed by this implementation.</remarks>
    public async Task<TResult> RunAsync<TResult>(TValue value, Func<Task<TResult>> action,
                                                 CancellationToken? cancellationToken = null)
    {
        var original = _current.Value;
        _current.Value = value;

        try
        {
            return await action();
        }
        finally
        {
            _current.Value = original;
        }
    }
}
