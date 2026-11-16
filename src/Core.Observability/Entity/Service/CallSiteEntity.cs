using Anarchy.Toolkit.Core.Extension;

namespace Anarchy.Toolkit.Core.Observability.Entity.Service;

/// <summary>
/// Identifies where an observability call originated (class + method), used to derive consistent log prefixes and
/// metric names.
/// </summary>
/// <remarks>
/// Usually created via <see cref="Anarchy.Toolkit.Core.Observability.Extension.CallSiteExtension"/>
/// (<c>this.CalledFrom()</c>) and passed to the
/// <see cref="Anarchy.Toolkit.Core.Observability.Service.Observability.Observer"/> overloads that accept a call site.
/// </remarks>
/// <param name="ClassName">The name of the class the call originates from.</param>
/// <param name="MethodName">The name of the method the call originates from.</param>
public record CallSiteEntity(string ClassName, string MethodName)
{
    /// <summary>
    /// Builds a metric name prefix in the form <c>class_name.method_name</c> (both parts converted to snake case).
    /// </summary>
    /// <returns>The metric prefix, e.g. <c>order_service.process_async</c>.</returns>
    public string ToMetricPrefix()
    {
        return $"{ClassName.ToSnakeCase()}.{MethodName.ToSnakeCase()}";
    }

    /// <summary>
    /// Builds a log message prefix in the form <c>[ClassName → MethodName]</c>.
    /// </summary>
    /// <returns>The log prefix, e.g. <c>[OrderService → ProcessAsync]</c>.</returns>
    public string ToLogPrefix()
    {
        return $"[{ClassName} → {MethodName}]";
    }
}
