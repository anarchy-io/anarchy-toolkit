using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Serilog;
using System.Diagnostics;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// Single entry point for observability: structured logging through <see cref="Logger"/>, metrics through
/// <see cref="MeasurerBase"/> and ambient log properties through <see cref="LogContext"/>.
/// The observer owns its <see cref="Logger"/>: disposing the observer flushes and disposes it.
/// </summary>
public partial class Observer(ILogger logger, MeasurerBase measurer) : IDisposable
{
    /// <summary>
    /// The configured logger instance.
    /// </summary>
    public ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// The configured metrics implementation.
    /// </summary>
    public MeasurerBase MeasurerBase { get; } = measurer ?? throw new ArgumentNullException(nameof(measurer));

    /// <summary>
    /// Pushes ambient properties onto every log event written while the returned scope is open.
    /// </summary>
    public LogContext LogContext { get; } = new();

    public async Task<TResult> MonitoredActionAsync<TResult>(
        CallSiteEntity caller,
        Func<Task<TResult>> action,
        bool recordExecutionDuration = true,
        bool recordExecutionCount = true,
        bool logErrorAsWarning = false,
        CancellationToken? cancellationToken = null)
    {
        var timer = Stopwatch.StartNew();

        try
        {
            var result = await action();

            if (recordExecutionDuration) ExecutionRecordDuration(caller, timer.Elapsed, Tags.Success);
            if (recordExecutionCount) ExecutionIncrementCount(caller, 1, Tags.Success);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken?.IsCancellationRequested == true)
        {
            LogInformation(caller, "Service action cancelled");

            var cancelledTags = Tags.Failure.WithTag("cancelled", "true");
            if (recordExecutionDuration) ExecutionRecordDuration(caller, timer.Elapsed, cancelledTags);
            if (recordExecutionCount) ExecutionIncrementCount(caller, 1, cancelledTags);

            throw;
        }
        catch (Exception exception)
        {
            if (logErrorAsWarning)
            {
                LogWarning(exception, caller, "Service action failed");
            }
            else
            {
                LogError(exception, caller, "Service action failed");
            }

            if (recordExecutionDuration) ExecutionRecordDuration(caller, timer.Elapsed, Tags.Failure);
            if (recordExecutionCount) ExecutionIncrementCount(caller, 1, Tags.Failure);

            throw;
        }
    }

    /// <summary>
    /// Flushes and disposes the <see cref="Logger"/>, so buffered sinks (e.g. CloudWatch) deliver pending events.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            (Logger as IDisposable)?.Dispose();
        }
    }
}
