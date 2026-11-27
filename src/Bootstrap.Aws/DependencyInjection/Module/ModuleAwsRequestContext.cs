using Anarchy.Toolkit.Bootstrap.Aws.Core.Service;
using Anarchy.Toolkit.Bootstrap.Aws.Telemetry;
using Anarchy.Toolkit.Core.Composition.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Bootstrap.Aws.DependencyInjection.Module;

/// <summary>
/// Registers <see cref="IAmazonRequestContext"/> (<see cref="AmazonRequestContextAccessor"/>) as a singleton, unless
/// one is already registered.
/// </summary>
/// <remarks>
/// The Lambda web entry point of <c>Anarchy.Toolkit.Runtime.Web.Aws</c> applies it for you.
/// </remarks>
public sealed class ModuleAwsRequestContext : IDependencyInjectionModule
{
    /// <inheritdoc />
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        services.TryAddSingleton<IAmazonRequestContext, AmazonRequestContextAccessor>();
        return services;
    }
}
