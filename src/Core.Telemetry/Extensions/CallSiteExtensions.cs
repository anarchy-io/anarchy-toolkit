using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Captures a call site using the caller's runtime type and member name.</summary>
public static class CallSiteExtensions
{
    /// <summary>Captures the current member of <paramref name="caller"/>.</summary>
    public static CallSite CalledFrom(this object caller, [CallerMemberName] string methodName = "") =>
        new(caller.GetType().Name, methodName);

}
