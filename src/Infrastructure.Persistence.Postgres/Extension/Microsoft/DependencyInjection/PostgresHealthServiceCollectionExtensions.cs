using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Health.Report.Postgres;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Extension.Microsoft.DependencyInjection;

/// <summary>
///     Registration helpers for PostgreSQL-backed service health reports.
/// </summary>
public static class PostgresHealthServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the PostgreSQL reader and writer health reports and exposes them as an
    ///     <see cref="IReadOnlyCollection{T}" /> of <see cref="IServiceHealthReport" />.
    /// </summary>
    /// <param name="services">The service collection to add the reports to.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddPostgresHealthReports(this IServiceCollection services)
    {
        return services.AddDbHealthReports<PostgresHealthReportReader, PostgresHealthReportWriter>();
    }
}
