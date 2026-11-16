using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Observability;
using Serilog;
using Serilog.Configuration;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Observability;

/// <summary>
/// Serilog enrichment extensions that add correlation information to log events.
/// </summary>
public static class EnvironmentLoggerConfigurationExtensions
{
    /// <summary>
    /// Enriches every log event with the current correlation and attempt ids.
    /// </summary>
    /// <remarks>
    /// Values are read from
    /// <see cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service.CorrelationContext"/> at the time each
    /// event is written. Properties already present on the event are not overwritten.
    /// </remarks>
    /// <param name="enrichmentConfiguration">The logger enrichment configuration.</param>
    /// <param name="correlationIdPropertyName">The log property holding the correlation id.</param>
    /// <param name="attemptIdPropertyName">The log property holding the attempt id.</param>
    /// <returns>The <see cref="LoggerConfiguration"/>, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="enrichmentConfiguration"/> is <see langword="null"/>.</exception>
    /// <exception cref="EmptyArgumentException">
    /// <paramref name="correlationIdPropertyName"/> or <paramref name="attemptIdPropertyName"/> is null, empty or
    /// whitespace.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    /// Log.Logger = new LoggerConfiguration()
    ///     .Enrich.WithCorrelationId()
    ///     .WriteTo.Console()
    ///     .CreateLogger();
    /// </code>
    /// </example>
    public static LoggerConfiguration WithCorrelationId(
        this LoggerEnrichmentConfiguration enrichmentConfiguration,
        string correlationIdPropertyName = CorrelationLogPropertyNames.CorrelationId,
        string attemptIdPropertyName = CorrelationLogPropertyNames.AttemptId)
    {
        if (enrichmentConfiguration == null)
            throw new ArgumentNullException(nameof(enrichmentConfiguration));
        if (string.IsNullOrWhiteSpace(correlationIdPropertyName))
            throw new EmptyArgumentException(nameof(correlationIdPropertyName), "Property name cannot be empty.");
        if (string.IsNullOrWhiteSpace(attemptIdPropertyName))
            throw new EmptyArgumentException(nameof(attemptIdPropertyName), "Property name cannot be empty.");

        return enrichmentConfiguration.With(
            new CorrelationIdEnricher(correlationIdPropertyName, attemptIdPropertyName));
    }
}
