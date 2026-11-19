using Anarchy.Toolkit.Infrastructure.Persistence.Db.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Health.Report.Mysql;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Extension.Microsoft.DependencyInjection;

/// <summary>
/// Registration helpers for MySQL-backed service health reports.
/// </summary>
public static class MysqlHealthServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MySQL reader and writer health reports and exposes them as an
    /// <see cref="IReadOnlyCollection{T}"/> of <c>IServiceHealthReport</c>.
    /// </summary>
    /// <param name="services">The service collection to add the reports to.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <remarks>
    /// Registers <see cref="MysqlHealthReportReader"/> and <see cref="MysqlHealthReportWriter"/> (scoped). Requires the
    /// keyed <c>"reader"</c>/<c>"writer"</c> storage policies registered by <c>ModuleMysql</c>, plus an <see cref="Anarchy.Toolkit.Core.Telemetry.IAppTelemetry"/>.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// services.AddMysqlHealthReports();
    /// </code>
    /// </example>
    public static IServiceCollection AddMysqlHealthReports(this IServiceCollection services) =>
        services.AddDbHealthReports<MysqlHealthReportReader, MysqlHealthReportWriter>();
}
