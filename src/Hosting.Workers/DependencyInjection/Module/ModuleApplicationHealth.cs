using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Anarchy.Toolkit.Hosting.Workers.DependencyInjection.Module;

/// <summary>
/// Registers the application health endpoints, reading the build metadata embedded in the given assembly.
/// </summary>
public sealed class ModuleApplicationHealth(Assembly assembly) : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        services.AddApplicationHealthEndpoints(assembly);

        return services;
    }
}
