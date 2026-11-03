using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Health.Report.Oracle;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Extension.Microsoft.DependencyInjection;

/// <summary>
///     Registration helpers for Oracle-backed service health reports.
/// </summary>
public static class OracleHealthServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the Oracle reader and writer health reports and exposes them as an
    ///     <see cref="IReadOnlyCollection{T}" /> of <see cref="IServiceHealthReport" />.
    /// </summary>
    /// <param name="services">The service collection to add the reports to.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddOracleHealthReports(this IServiceCollection services)
    {
        return services.AddDbHealthReports<OracleHealthReportReader, OracleHealthReportWriter>();
    }
}
