using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;
using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.Serilog.Enrichers;

internal sealed class CorrelationIdEnricher(string correlationIdPropertyName, string attemptIdPropertyName)
    : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (CorrelationContext.CorrelationId is { } correlationId)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(correlationIdPropertyName, correlationId));
        }

        if (CorrelationContext.AttemptId is { } attemptId)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(attemptIdPropertyName, attemptId));
        }
    }
}
