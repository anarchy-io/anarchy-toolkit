using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Extensions.Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.DependencyInjection.Module;

/// <summary>
/// Registers the Postgres reader/writer service health reports.
/// </summary>
/// <remarks>
/// Delegates to <c>AddPostgresHealthReports()</c>. Use together with <see cref="ModulePostgres"/>, which registers the storage
/// policies the reports depend on.
/// </remarks>
public sealed class ModulePostgresHealthReport : IDependencyInjectionModule
{
    /// <inheritdoc/>
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        return services.AddPostgresHealthReports();
    }
}
