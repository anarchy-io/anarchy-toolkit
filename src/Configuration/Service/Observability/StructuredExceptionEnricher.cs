using Serilog.Core;
using Serilog.Events;

namespace Anarchy.Toolkit.Configuration.Service.Observability;

internal class StructuredExceptionEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var exception = logEvent.Exception;
        if (exception == null)
        {
            return;
        }

        var structuredException = new {
            Class = exception.GetType().FullName,
            Level = (int)logEvent.Level,
            exception.Source,
            exception.Message,
            exception.Data,
            MethodName = exception.TargetSite?.Name,
            MethodClass = exception.TargetSite?.ReflectedType?.FullName,
            InnerClass = exception.InnerException?.GetType().FullName,
            InnerSource = exception.InnerException?.Source,
            InnerMessage = exception.InnerException?.Message,
            InnerMethodName = exception.InnerException?.TargetSite?.Name,
            InnerMethodClass = exception.InnerException?.TargetSite?.ReflectedType?.FullName,
            Trace = exception.StackTrace
        };

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("Exceptions", structuredException, true));
    }
}
