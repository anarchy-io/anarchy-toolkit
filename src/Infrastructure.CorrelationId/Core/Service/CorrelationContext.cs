using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;

/// <summary>
/// Ambient, async-flowing holder of the current correlation and attempt identifiers.
/// </summary>
/// <remarks>
/// <para>
/// Values are stored in an <see cref="System.Threading.AsyncLocal{T}"/>, so they flow with the logical execution
/// context (across <see langword="await"/>s and child tasks) and are isolated between concurrent requests. Changes made
/// in a child async flow are not visible to the caller.
/// </para>
/// <para>
/// The values are set per request by
/// <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware.CorrelationIdMiddleware"/>.
/// Background workers and message consumers can call <see cref="BeginScope"/> to set the values and open a structured
/// logging scope for the duration of their operation.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using (CorrelationContext.BeginScope(logger, correlationId, attemptId))
/// {
///     logger.LogInformation("Processing message");
/// }
/// </code>
/// </example>
public static class CorrelationContext
{
    /// <summary>
    /// Gets the correlation id of the current async flow, or <see langword="null"/> when none has been set.
    /// </summary>
    public static string? CorrelationId => Values.Value?.CorrelationId;

    /// <summary>
    /// Gets the attempt id of the current async flow, or <see langword="null"/> when none has been set.
    /// </summary>
    public static string? AttemptId => Values.Value?.AttemptId;

    /// <summary>
    /// Sets the correlation and attempt identifiers for the current async flow, replacing any previous values.
    /// </summary>
    /// <param name="correlationId">The correlation id, or <see langword="null"/> to clear it.</param>
    /// <param name="attemptId">The attempt id, or <see langword="null"/> to clear it.</param>
    public static void SetCorrelationValues(string? correlationId, string? attemptId)
    {
        var values = new CorrelationValues(correlationId: correlationId, attemptId: attemptId);

        Values.Value = values;
    }

    /// <summary>
    /// Sets the correlation and attempt identifiers, adds them as tags to the current activity, and opens a structured
    /// logging scope. Dispose the returned scope when the operation completes to restore the previous async context;
    /// tags remain on the activity for trace export unless this scope replaced existing tags.
    /// </summary>
    /// <param name="logger">The logger whose provider should receive the structured scope.</param>
    /// <param name="correlationId">The correlation id, or <see langword="null"/>.</param>
    /// <param name="attemptId">The attempt id, or <see langword="null"/>.</param>
    /// <param name="correlationIdPropertyName">The log and activity tag name for the correlation id.</param>
    /// <param name="attemptIdPropertyName">The log and activity tag name for the attempt id.</param>
    /// <returns>A disposable scope that restores the prior correlation context and activity tags.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A property name is null, empty, whitespace, or equal to the other name.</exception>
    public static IDisposable BeginScope(
        Microsoft.Extensions.Logging.ILogger logger,
        string? correlationId,
        string? attemptId,
        string correlationIdPropertyName = CorrelationLogPropertyNames.CorrelationId,
        string attemptIdPropertyName = CorrelationLogPropertyNames.AttemptId)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationIdPropertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(attemptIdPropertyName);
        if (string.Equals(correlationIdPropertyName, attemptIdPropertyName, StringComparison.Ordinal))
            throw new ArgumentException("Correlation and attempt property names must be different.",
                nameof(attemptIdPropertyName));

        var previousValues = Values.Value;
        var activity = System.Diagnostics.Activity.Current;
        var previousCorrelationTag = activity?.GetTagItem(correlationIdPropertyName);
        var previousAttemptTag = activity?.GetTagItem(attemptIdPropertyName);
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal) {
            [correlationIdPropertyName] = correlationId,
            [attemptIdPropertyName] = attemptId
        };
        var loggingScope = logger.BeginScope(properties);

        try
        {
            SetCorrelationValues(correlationId, attemptId);
            if (correlationId is not null)
                activity?.SetTag(correlationIdPropertyName, correlationId);
            if (attemptId is not null)
                activity?.SetTag(attemptIdPropertyName, attemptId);

            return new CorrelationScope(previousValues, activity, correlationIdPropertyName, previousCorrelationTag,
                attemptIdPropertyName, previousAttemptTag, loggingScope);
        }
        catch
        {
            try
            {
                if (correlationId is not null)
                    activity?.SetTag(correlationIdPropertyName, previousCorrelationTag);
                if (attemptId is not null)
                    activity?.SetTag(attemptIdPropertyName, previousAttemptTag);
            }
            finally
            {
                Values.Value = previousValues;
            }

            try
            {
                loggingScope?.Dispose();
            }
            catch
            {
                // Preserve the exception that prevented scope initialization.
            }

            throw;
        }
    }

    private static readonly AsyncLocal<CorrelationValues?> Values = new();

    private sealed class CorrelationScope(
        CorrelationValues? previousValues,
        System.Diagnostics.Activity? activity,
        string correlationIdPropertyName,
        object? previousCorrelationTag,
        string attemptIdPropertyName,
        object? previousAttemptTag,
        IDisposable? loggingScope) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            try
            {
                loggingScope?.Dispose();
            }
            finally
            {
                try
                {
                    if (previousCorrelationTag is not null)
                        activity?.SetTag(correlationIdPropertyName, previousCorrelationTag);
                    if (previousAttemptTag is not null)
                        activity?.SetTag(attemptIdPropertyName, previousAttemptTag);
                }
                finally
                {
                    Values.Value = previousValues;
                }
            }
        }
    }

    private class CorrelationValues
    {
        public string? CorrelationId { get; }
        public string? AttemptId { get; }

        public CorrelationValues(string? correlationId, string? attemptId)
        {
            CorrelationId = correlationId;
            AttemptId = attemptId;
        }
    }
}
