namespace Anarchy.Toolkit.Core.Helper;

/// <summary>
/// Concurrency helpers for running asynchronous work in parallel.
/// </summary>
public static class ThreadHelper
{
    private static ICollection<Task<TResult>> TaskCollection<TParam, TResult>(
        IReadOnlyCollection<TParam> callbackArgsCollection,
        Func<TParam, Task<TResult>> callbackFunc,
        CancellationToken cancellationToken,
        SemaphoreSlim throttler)
    {
        return callbackArgsCollection.Select(callbackArgsChunk => Task.Run(async () => {
            await throttler.WaitAsync(cancellationToken);

            try
            {
                return await callbackFunc(callbackArgsChunk);
            }
            finally
            {
                throttler.Release();
            }
        }, cancellationToken))
        .ToList();
    }

    /// <summary>
    /// Runs <paramref name="callbackFunc"/> once per element of <paramref name="callbackArgsCollection"/> on the thread
    /// pool, with at most <paramref name="maxConcurrentExecutions"/> invocations running at the same time.
    /// </summary>
    /// <remarks>
    /// All tasks are scheduled upfront and throttled with a <see cref="SemaphoreSlim"/>. Results are collected in
    /// <b>completion order</b>, not input order. The first faulted task stops the wait and its exception is rethrown
    /// (other tasks keep running in the background).
    /// </remarks>
    /// <typeparam name="TParam">The type of each callback argument.</typeparam>
    /// <typeparam name="TResult">The callback result type.</typeparam>
    /// <param name="callbackArgsCollection">The arguments; one invocation per element.</param>
    /// <param name="callbackFunc">The asynchronous callback to run.</param>
    /// <param name="maxConcurrentExecutions">The maximum number of concurrent invocations; must be positive.</param>
    /// <param name="cancellationToken">Cancels pending invocations.</param>
    /// <returns>The results of every invocation, in completion order.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    /// <example>
    /// <code language="csharp">
    /// var users = await ThreadHelper.ExecuteInParallelWithThrottlingAsync(
    ///     userIds, id =&gt; client.GetUserAsync(id), maxConcurrentExecutions: 4, cancellationToken);
    /// </code>
    /// </example>
    public static async Task<IReadOnlyCollection<TResult>> ExecuteInParallelWithThrottlingAsync<TParam, TResult>(
        IReadOnlyCollection<TParam> callbackArgsCollection,
        Func<TParam, Task<TResult>> callbackFunc,
        int maxConcurrentExecutions,
        CancellationToken cancellationToken)
    {
        var throttler = new SemaphoreSlim(maxConcurrentExecutions);
        var tasks = TaskCollection(callbackArgsCollection, callbackFunc, cancellationToken, throttler);
        var results = new List<TResult>();

        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);

            if (completed.IsFaulted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw completed.Exception!.InnerException!;
            }

            results.Add(await completed); // Will rethrow if faulted or cancelled
        }

        return results;
    }
}
