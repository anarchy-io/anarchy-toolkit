using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Extension.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Infrastructure.CorrelationId.DependencyInjection.Module;

/// <summary>
/// Registers the <see cref="CorrelationIdOptions"/>, using the defaults unless a factory resolves them from the
/// application's configuration.
/// </summary>
public sealed class ModuleCorrelationId(Func<IConfiguration, CorrelationIdOptions>? options = null)
    : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        services.AddCorrelationId(options?.Invoke(configuration) ?? new CorrelationIdOptions());

        return services;
    }
}
