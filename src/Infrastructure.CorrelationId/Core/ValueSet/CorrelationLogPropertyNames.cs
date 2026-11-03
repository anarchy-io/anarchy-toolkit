namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;

/// <summary>
/// Default log property names written by the correlation-id Serilog enricher.
/// </summary>
public static class CorrelationLogPropertyNames
{
    public const string CorrelationId = "CorrelationId";
    public const string AttemptId = "AttemptId";
}
