namespace Anarchy.Toolkit.Infrastructure.CorrelationId.Core.ValueSet;

/// <summary>
/// Default log property names written by the correlation-id Serilog enricher.
/// </summary>
/// <seealso cref="Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Observability.EnvironmentLoggerConfigurationExtensions"/>
public static class CorrelationLogPropertyNames
{
    /// <summary>Default log property holding the correlation id: <c>CorrelationId</c>.</summary>
    public const string CorrelationId = "CorrelationId";

    /// <summary>Default log property holding the attempt id: <c>AttemptId</c>.</summary>
    public const string AttemptId = "AttemptId";
}
