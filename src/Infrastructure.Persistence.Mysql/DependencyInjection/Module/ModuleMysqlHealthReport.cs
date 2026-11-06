using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Extension.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.DependencyInjection.Module;

public sealed class ModuleMysqlHealthReport : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) =>
        services.AddMysqlHealthReports();
}
