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
    /// <typeparam name="TReaderReport">Health report for the reader instance (for example a <c>DbHealthReportBase</c> subclass).</typeparam>
    /// <typeparam name="TWriterReport">Health report for the writer instance.</typeparam>
    /// <param name="services">The service collection to add the reports to.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <remarks>
    ///     All registrations are scoped. The collection registration replaces nothing: calling this method
    ///     more than once adds further <see cref="IReadOnlyCollection{T}" /> registrations, and the last one wins
    ///     when resolved.
    /// </remarks>
    /// <example>
    ///     <code language="csharp">
    ///     services.AddDbHealthReports&lt;MyReaderHealthReport, MyWriterHealthReport&gt;();
    ///     </code>
    /// </example>
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
