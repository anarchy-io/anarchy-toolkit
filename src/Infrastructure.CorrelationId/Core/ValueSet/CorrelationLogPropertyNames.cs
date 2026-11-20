namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;

/// <summary>
/// Default structured log property names for correlation and attempt identifiers.
/// </summary>
/// <seealso cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service.CorrelationContext.BeginScope"/>
public static class CorrelationLogPropertyNames
{
    /// <summary>Default log property holding the correlation id: <c>CorrelationId</c>.</summary>
    public const string CorrelationId = "CorrelationId";

    /// <summary>Default log property holding the attempt id: <c>AttemptId</c>.</summary>
    public const string AttemptId = "AttemptId";
}
