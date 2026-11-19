using Anarchy.Toolkit.Core.Extension;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Identifies the class and method responsible for a telemetry operation.</summary>
/// <param name="ClassName">The caller's class name.</param>
/// <param name="MethodName">The caller's method name.</param>
public sealed record CallSite(string ClassName, string MethodName)
{
    /// <summary>Returns the standardized metric prefix for this call site.</summary>
    public string ToMetricPrefix() => $"{ClassName.ToSnakeCase()}.{MethodName.ToSnakeCase()}";

    /// <summary>Returns the standardized log prefix for this call site.</summary>
    public string ToLogPrefix() => $"[{ClassName} → {MethodName}]";
}
