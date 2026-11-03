namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;

/// <summary>
///     For values that are scoped to the logical async call chain, not just the thread.
/// </summary>
/// <typeparam name="TValue">The type of value saved</typeparam>
public interface IAsyncFlowContext<TValue>
{
    TValue Value { get; }

    bool HasValue { get; }

    bool TryGetValue(out TValue? value, TValue? defaultValue = default);

    void Clear();

    void Run(TValue value, Action action);

    Task<TResult> RunAsync<TResult>(TValue value, Func<Task<TResult>> action,
                                    CancellationToken? cancellationToken = null);
}
