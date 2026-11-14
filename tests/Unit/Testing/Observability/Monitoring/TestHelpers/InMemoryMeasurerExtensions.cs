using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using System.Reflection;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.TestHelpers;

internal static class InMemoryMeasurerExtensions
{
    private static readonly PropertyInfo MetricsProperty =
        typeof(InMemoryMeasurer).GetProperty("Metrics", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("InMemoryMeasurer.Metrics property not found.");

    /// <summary>
    /// Reads the internal <c>InMemoryMeasurer.Metrics</c> collection. The legacy test project accessed it through
    /// InternalsVisibleTo, which the toolkit does not grant to this assembly.
    /// </summary>
    public static List<Metric> GetMetrics(this InMemoryMeasurer metrics) =>
        (List<Metric>)MetricsProperty.GetValue(metrics)!;
}
