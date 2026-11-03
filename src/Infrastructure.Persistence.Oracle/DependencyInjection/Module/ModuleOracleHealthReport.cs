using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Extension.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.DependencyInjection.Module;

/// <summary>
/// Registers the Oracle reader/writer service health reports.
/// </summary>
public sealed class ModuleOracleHealthReport : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        return services.AddOracleHealthReports();
    }
}
