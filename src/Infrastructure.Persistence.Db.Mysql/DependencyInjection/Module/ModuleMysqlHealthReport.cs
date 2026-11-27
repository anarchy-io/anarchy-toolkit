using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Extensions.Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.DependencyInjection.Module;

/// <summary>
/// Registers the MySQL reader/writer service health reports.
/// </summary>
/// <remarks>
/// Delegates to <c>AddMysqlHealthReports()</c>. Use together with <see cref="ModuleMysql"/>, which registers the storage
/// policies the reports depend on.
/// </remarks>
public sealed class ModuleMysqlHealthReport : IDependencyInjectionModule
{
    /// <inheritdoc/>
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) =>
        services.AddMysqlHealthReports();
}
