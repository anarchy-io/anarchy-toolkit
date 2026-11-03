namespace Anarchy.Toolkit.Core.Helper;

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
