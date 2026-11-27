using Anarchy.Toolkit.Infrastructure.Persistence.Db.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Health.Report.Mysql;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Extensions.Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration helpers for MySQL-backed service health reports.
/// </summary>
public static class MysqlHealthServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MySQL reader and writer health reports and exposes them as an
    /// <see cref="IReadOnlyCollection{T}"/> of <c>IHealthCheck</c>.
    /// </summary>
    /// <param name="services">The service collection to add the reports to.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <remarks>
    /// Registers <see cref="MysqlHealthReportReader"/> and <see cref="MysqlHealthReportWriter"/> (scoped). Requires the
    /// keyed <c>"reader"</c>/<c>"writer"</c> storage policies registered by <c>ModuleMysql</c>, plus an <see cref="Anarchy.Toolkit.Core.Telemetry.AppTelemetry"/>.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// services.AddMysqlHealthReports();
    /// </code>
    /// </example>
    public static IServiceCollection AddMysqlHealthReports(this IServiceCollection services) =>
        services.AddDbHealthReports<MysqlHealthReportReader, MysqlHealthReportWriter>();
}
