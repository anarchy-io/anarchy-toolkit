namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;

/// <summary>
///     For values that are scoped to the logical async call chain, not just the thread.
/// </summary>
/// <remarks>
///     Used by unit-of-work implementations to share the active <see cref="Entity.Storage.StorageConnectionEnvelopeBase{T}"/>
///     with storage policies running in the same async flow. The default implementation is
///     <see cref="AsyncLocalFlowContext{TValue}"/>.
/// </remarks>
/// <typeparam name="TValue">The type of value saved</typeparam>
public interface IAsyncFlowContext<TValue>
{
    /// <summary>
    ///     Gets the value of the current async flow.
    /// </summary>
    /// <exception cref="Exceptions.EmptyAsyncContextException">No value is set in the current async flow.</exception>
    TValue Value { get; }

    /// <summary>
    ///     Gets whether a value is set in the current async flow.
    /// </summary>
    bool HasValue { get; }

    /// <summary>
    ///     Tries to get the value of the current async flow.
    /// </summary>
    /// <param name="value">The current value, or <paramref name="defaultValue"/> when none is set.</param>
    /// <param name="defaultValue">The value returned through <paramref name="value"/> when none is set.</param>
    /// <returns><see langword="true"/> when a value is set.</returns>
    bool TryGetValue(out TValue? value, TValue? defaultValue = default);

    /// <summary>
    ///     Clears the value of the current async flow.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Runs <paramref name="action"/> with <paramref name="value"/> set as the current value, restoring the previous
    ///     value afterwards.
    /// </summary>
    /// <param name="value">The value to expose during <paramref name="action"/>.</param>
    /// <param name="action">The action to run.</param>
    void Run(TValue value, Action action);

    /// <summary>
    ///     Runs <paramref name="action"/> with <paramref name="value"/> set as the current value, restoring the previous
    ///     value afterwards.
    /// </summary>
    /// <typeparam name="TResult">The result type of <paramref name="action"/>.</typeparam>
    /// <param name="value">The value to expose during <paramref name="action"/>.</param>
    /// <param name="action">The asynchronous action to run.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The result of <paramref name="action"/>.</returns>
    Task<TResult> RunAsync<TResult>(TValue value, Func<Task<TResult>> action,
                                    CancellationToken? cancellationToken = null);
}
