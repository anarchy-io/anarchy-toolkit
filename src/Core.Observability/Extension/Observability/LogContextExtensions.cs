using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Anarchy.Toolkit.Core.Observability.Service.Observability;

namespace Anarchy.Toolkit.Core.Observability.Extension.Observability;

public static class LogContextExtensions
{
    public static LogContextProperty[] ForCorrelationId(string? correlationId = null)
    {
        return [
            new LogContextProperty("CorrelationId", correlationId ?? Guid.NewGuid().ToString())
        ];
    }

    public static LogContextProperty[] ForCallerSite(CallSiteEntity callerSite)
    {
        return [
            new LogContextProperty("ClassName", callerSite.ClassName),
            new LogContextProperty("MethodName", callerSite.MethodName)
        ];
    }

    extension(LogContext logContext)
    {
        public IDisposable WithCorrelationId(string? correlationId = null)
        {
            return logContext.PushProperties(ForCorrelationId(correlationId));
        }

        public IDisposable WithCallerSite(CallSiteEntity callerSite)
        {
            return logContext.PushProperties(ForCallerSite(callerSite));
        }
    }
}
