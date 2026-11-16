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
/// <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Host.Middleware.CorrelationIdMiddleware"/> and read by
/// the Serilog enricher registered through
/// <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Observability.EnvironmentLoggerConfigurationExtensions.WithCorrelationId"/>.
/// Background workers or message consumers can call <see cref="SetCorrelationValues"/> themselves.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// CorrelationContext.SetCorrelationValues(message.CorrelationId, Guid.NewGuid().ToString());
/// logger.LogInformation("Processing {Id}", CorrelationContext.CorrelationId);
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

    private static readonly AsyncLocal<CorrelationValues?> Values = new();

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
