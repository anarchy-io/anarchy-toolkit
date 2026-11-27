using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Extensions.Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.DependencyInjection.Module;

/// <summary>
/// Registers the Oracle reader/writer service health reports.
/// </summary>
/// <remarks>
/// Delegates to <c>AddOracleHealthReports()</c>. Use together with <see cref="ModuleOracle"/>, which registers the storage
/// policies the reports depend on.
/// </remarks>
public sealed class ModuleOracleHealthReport : IDependencyInjectionModule
{
    /// <inheritdoc/>
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        return services.AddOracleHealthReports();
    }
}
