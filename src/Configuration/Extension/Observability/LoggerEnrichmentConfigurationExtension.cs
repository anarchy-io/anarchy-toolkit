using Anarchy.Toolkit.Configuration.Service.Observability;
using Serilog;
using Serilog.Configuration;

namespace Anarchy.Toolkit.Configuration.Extension.Observability;

public static class LoggerEnrichmentConfigurationExtension
{
    extension(LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        public LoggerConfiguration WithAmazonRequestContext()
        {
            return enrichmentConfiguration.With<AmazonTraceIdEnricher>();
        }

        public LoggerConfiguration WithStructuredExceptionContext()
        {
            return enrichmentConfiguration.With<StructuredExceptionEnricher>();
        }
    }
}
