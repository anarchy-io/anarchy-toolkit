using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;
using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Service.Observability
{
    internal class CorrelationIdEnricher(string correlationIdPropertyName, string attemptIdPropertyName)
        : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(correlationIdPropertyName,
                CorrelationContext.CorrelationId));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(attemptIdPropertyName,
                CorrelationContext.AttemptId));
        }
    }
}
