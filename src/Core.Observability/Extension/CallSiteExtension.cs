using Anarchy.Toolkit.Core.Observability.Entity.Service;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Core.Observability.Extension;

public static class CallSiteExtension
{
    extension(object caller)
    {
        public CallSiteEntity CalledFrom([CallerMemberName] string methodName = "")
        {
            return new CallSiteEntity(caller.GetType().Name, methodName);
        }

        public CallSiteEntity ParentCaller()
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(2);
            var method = frame?.GetMethod();
            var parentClassName = method?.DeclaringType?.Name ?? "Unknown";
            return new CallSiteEntity(parentClassName, method?.Name ?? "Unknown");
        }
    }
}
