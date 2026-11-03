namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;

/// <summary>
/// Default HTTP header names used by <c>CorrelationIdOptions</c>. Override them through the options when the
/// organisation uses its own convention.
/// </summary>
public static class CorrelationHeaderNames
{
    public const string CorrelationId = "X-Correlation-Id";
    public const string AttemptId = "X-Attempt-Id";
}
