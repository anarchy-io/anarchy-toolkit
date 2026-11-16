namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;

/// <summary>
/// Default HTTP header names used by <c>CorrelationIdOptions</c>. Override them through the options when the
/// organisation uses its own convention.
/// </summary>
/// <seealso cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity.CorrelationIdOptions"/>
public static class CorrelationHeaderNames
{
    /// <summary>
    /// Default correlation id header: <c>X-Correlation-Id</c>. Identifies a logical operation across services.
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>
    /// Default attempt id header: <c>X-Attempt-Id</c>. Identifies a single attempt (e.g. a retry) of the operation.
    /// </summary>
    public const string AttemptId = "X-Attempt-Id";
}
