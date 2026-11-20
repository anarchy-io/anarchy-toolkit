using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog.Enrichers;
using Serilog;
using Serilog.Configuration;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;

/// <summary>Optional Serilog enrichment for provider-neutral correlation context.</summary>
public static class CorrelationIdEnrichmentExtensions
{
    /// <summary>
    /// Adds correlation and attempt identifiers from the ambient correlation context when the log event does not
    /// already have those properties. This complements <c>ILogger.BeginScope</c> without duplicating properties that
    /// a Serilog logging provider has already added from the Microsoft logging scope.
    /// </summary>
    /// <param name="enrichment">The Serilog enrichment configuration.</param>
    /// <param name="correlationIdPropertyName">The log property holding the correlation id.</param>
    /// <param name="attemptIdPropertyName">The log property holding the attempt id.</param>
    /// <returns>The logger configuration, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="enrichment"/> is <see langword="null"/>.</exception>
    /// <exception cref="EmptyArgumentException">A property name is null, empty or whitespace.</exception>
    /// <exception cref="ArgumentException">The property names are the same.</exception>
    public static LoggerConfiguration WithCorrelationId(
        this LoggerEnrichmentConfiguration enrichment,
        string correlationIdPropertyName = CorrelationLogPropertyNames.CorrelationId,
        string attemptIdPropertyName = CorrelationLogPropertyNames.AttemptId)
    {
        ArgumentNullException.ThrowIfNull(enrichment);
        if (string.IsNullOrWhiteSpace(correlationIdPropertyName))
            throw new EmptyArgumentException(nameof(correlationIdPropertyName), "Property name cannot be empty.");
        if (string.IsNullOrWhiteSpace(attemptIdPropertyName))
            throw new EmptyArgumentException(nameof(attemptIdPropertyName), "Property name cannot be empty.");
        if (string.Equals(correlationIdPropertyName, attemptIdPropertyName, StringComparison.Ordinal))
            throw new ArgumentException("Correlation and attempt property names must be different.", nameof(attemptIdPropertyName));

        return enrichment.With(new CorrelationIdEnricher(correlationIdPropertyName, attemptIdPropertyName));
    }
}
