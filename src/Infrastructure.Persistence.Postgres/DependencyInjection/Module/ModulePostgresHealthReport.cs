using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Extension.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.DependencyInjection.Module;

/// <summary>
/// Registers the Postgres reader/writer service health reports.
/// </summary>
public sealed class ModulePostgresHealthReport : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        return services.AddPostgresHealthReports();
    }
}
