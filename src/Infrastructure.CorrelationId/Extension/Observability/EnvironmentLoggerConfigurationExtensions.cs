using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Observability;
using Serilog;
using Serilog.Configuration;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Observability
{
    public static class EnvironmentLoggerConfigurationExtensions
    {
        /// <summary>
        /// Enriches every log event with the current correlation and attempt ids.
        /// </summary>
        /// <param name="enrichmentConfiguration">The logger enrichment configuration.</param>
        /// <param name="correlationIdPropertyName">The log property holding the correlation id.</param>
        /// <param name="attemptIdPropertyName">The log property holding the attempt id.</param>
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
}
