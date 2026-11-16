using Anarchy.Toolkit.Configuration.Service.Observability;
using Serilog;
using Serilog.Configuration;

namespace Anarchy.Toolkit.Configuration.Extension.Observability;

/// <summary>
/// Serilog enrichment extensions provided by the toolkit.
/// </summary>
/// <example>
/// <code language="csharp">
/// var logger = new LoggerConfiguration()
///     .Enrich.WithAmazonRequestContext()
///     .Enrich.WithStructuredExceptionContext()
///     .WriteTo.Console()
///     .CreateLogger();
/// </code>
/// </example>
public static class LoggerEnrichmentConfigurationExtension
{
    /// <param name="enrichmentConfiguration">The Serilog enrichment configuration.</param>
    extension(LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        /// <summary>
        /// Adds an <c>X-Amzn-Trace-Id</c> property with the current
        /// <see cref="Anarchy.Toolkit.Configuration.Entity.Observability.AmazonRequestContext.AmazonTraceId"/> to each
        /// event (if not already present).
        /// </summary>
        /// <returns>The logger configuration, for chaining.</returns>
        public LoggerConfiguration WithAmazonRequestContext()
        {
            return enrichmentConfiguration.With<AmazonTraceIdEnricher>();
        }

        /// <summary>
        /// Adds an <c>Exceptions</c> property to events that carry an exception, with a structured view of it (type,
        /// level, source, message, data, target method/class, first inner exception details and stack trace).
        /// </summary>
        /// <returns>The logger configuration, for chaining.</returns>
        public LoggerConfiguration WithStructuredExceptionContext()
        {
            return enrichmentConfiguration.With<StructuredExceptionEnricher>();
        }
    }
}
