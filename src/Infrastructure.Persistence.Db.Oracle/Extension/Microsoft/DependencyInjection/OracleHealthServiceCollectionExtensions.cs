using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Health.Report.Oracle;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Extension.Microsoft.DependencyInjection;

/// <summary>
///     Registration helpers for Oracle-backed service health reports.
/// </summary>
public static class OracleHealthServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the Oracle reader and writer health reports and exposes them as an
    ///     <see cref="IReadOnlyCollection{T}" /> of <see cref="IHealthCheck" />.
    /// </summary>
    /// <param name="services">The service collection to add the reports to.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <remarks>
    /// Registers <see cref="OracleHealthReportReader"/> and <see cref="OracleHealthReportWriter"/> (scoped). Requires the
    /// keyed <c>"reader"</c>/<c>"writer"</c> storage policies registered by <c>ModuleOracle</c>, plus an <see cref="Anarchy.Toolkit.Core.Telemetry.AppTelemetry"/>.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// services.AddOracleHealthReports();
    /// </code>
    /// </example>
    public static IServiceCollection AddOracleHealthReports(this IServiceCollection services)
    {
        return services.AddDbHealthReports<OracleHealthReportReader, OracleHealthReportWriter>();
    }
}
