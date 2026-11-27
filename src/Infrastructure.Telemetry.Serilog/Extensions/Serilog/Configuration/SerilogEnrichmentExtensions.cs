using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog.Enrichers;
using Serilog;
using Serilog.Configuration;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.Serilog.Extensions.Serilog.Configuration;

/// <summary>Optional Serilog enrichers provided by the toolkit.</summary>
/// <example>
/// <code language="csharp">
/// var logger = new LoggerConfiguration()
///     .Enrich.FromLogContext()
///     .Enrich.WithStructuredExceptionContext()
///     .WriteTo.Console()
///     .CreateLogger();
/// </code>
/// </example>
public static class SerilogEnrichmentExtensions
{
    /// <param name="enrichment">The Serilog enrichment configuration.</param>
    extension(LoggerEnrichmentConfiguration enrichment)
    {
        /// <summary>
        /// Adds an <c>Exceptions</c> property to events that carry an exception, with a structured view of it (type,
        /// level, source, message, data, target method and class, first inner exception and stack trace).
        /// </summary>
        /// <returns>The logger configuration, for chaining.</returns>
        public LoggerConfiguration WithStructuredExceptionContext() => enrichment.With<StructuredExceptionEnricher>();
    }
}
