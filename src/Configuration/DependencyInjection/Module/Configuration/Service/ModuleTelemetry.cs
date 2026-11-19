using Anarchy.Toolkit.Configuration.Core.Service;
using Anarchy.Toolkit.Configuration.Entity.Observability;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.DependencyInjection.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service;

/// <summary>Registers provider-neutral application telemetry, correlation context and application context.</summary>
/// <param name="correlationIdOptions">Optional factory for correlation-id options.</param>
public sealed class ModuleTelemetry(Func<IConfiguration, CorrelationIdOptions>? correlationIdOptions = null) : IDependencyInjectionModule
{
    /// <inheritdoc />
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) =>
        services
            .UseModule(new ModuleCorrelationId(correlationIdOptions), configuration, environment)
            .AddSingleton<IApplicationContext, ApplicationContext>()
            .AddLogging()
            .AddMetrics()
            .AddAppTelemetry();
}
