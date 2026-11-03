using Anarchy.Toolkit.Configuration.Entity.Observability;
using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Configuration.Service.Observability;

internal class AmazonTraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("X-Amzn-Trace-Id",
            AmazonRequestContext.AmazonTraceId));
    }
}
