using Anarchy.Toolkit.Core.Health.Service.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Extension.Microsoft.DependencyInjection;

/// <summary>
///     Registration helpers for database-backed service health reports.
/// </summary>
public static class DbHealthServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the given reader and writer health reports and exposes them as an
    ///     <see cref="IReadOnlyCollection{T}" /> of <see cref="IServiceHealthReport" />.
    /// </summary>
    /// <param name="services">The service collection to add the reports to.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddDbHealthReports<TReaderReport, TWriterReport>(
        this IServiceCollection services)
        where TReaderReport : class, IServiceHealthReport
        where TWriterReport : class, IServiceHealthReport
    {
        return services
            .AddScoped<TReaderReport>()
            .AddScoped<TWriterReport>()
            .AddScoped<IReadOnlyCollection<IServiceHealthReport>>(sp =>
            [
                sp.GetRequiredService<TReaderReport>(),
                sp.GetRequiredService<TWriterReport>(),
            ]);
    }
}
