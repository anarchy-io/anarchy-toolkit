using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Health.Report.Postgres;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Extensions.Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Registration helpers for PostgreSQL-backed service health reports.
/// </summary>
public static class PostgresHealthServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the PostgreSQL reader and writer health reports and exposes them as an
    ///     <see cref="IReadOnlyCollection{T}" /> of <see cref="IHealthCheck" />.
    /// </summary>
    /// <param name="services">The service collection to add the reports to.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <remarks>
    /// Registers <see cref="PostgresHealthReportReader"/> and <see cref="PostgresHealthReportWriter"/> (scoped). Requires the
    /// keyed <c>"reader"</c>/<c>"writer"</c> storage policies registered by <c>ModulePostgres</c>, plus an <see cref="Anarchy.Toolkit.Core.Telemetry.AppTelemetry"/>.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// services.AddPostgresHealthReports();
    /// </code>
    /// </example>
    public static IServiceCollection AddPostgresHealthReports(this IServiceCollection services)
    {
        return services.AddDbHealthReports<PostgresHealthReportReader, PostgresHealthReportWriter>();
    }
}
