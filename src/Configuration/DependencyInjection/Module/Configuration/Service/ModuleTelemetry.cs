using Anarchy.Toolkit.Configuration.Core.Service;
using Anarchy.Toolkit.Configuration.Telemetry;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.DependencyInjection.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service;

/// <summary>
/// Registers the toolkit's telemetry (<see cref="ICoreTelemetry"/>, <see cref="IMetricRecorder"/>,
/// <see cref="AppTelemetry"/>), the correlation context and the <see cref="IApplicationContext"/>.
/// </summary>
/// <remarks>
/// <para>The application decides where logs and metrics go, in dependency injection: register logging providers
/// (<c>AddLogging(...)</c>), a metric recorder or an <see cref="ICoreTelemetry"/> before or after this module, or
/// pass a <see cref="ModuleTelemetryBase"/> that does it. Defaults only fill in what is missing (see
/// <see cref="TelemetryServiceCollectionExtensions.AddCoreTelemetry"/>), and no console sink is added.</para>
/// </remarks>
/// <param name="coreTelemetry">The application's telemetry module; the defaults when <see langword="null"/>.</param>
/// <param name="correlationIdOptions">Resolves the correlation-id options; the defaults when <see langword="null"/>.</param>
/// <example>
/// <code language="csharp">
/// services.UseModule(
///     new ModuleLibConfiguration&lt;ModuleAppSettings, AppSettings&gt;(new ModuleTelemetry(new CompanyTelemetryModule())),
///     configuration, environment);
/// </code>
/// </example>
public sealed class ModuleTelemetry(
    ModuleTelemetryBase? coreTelemetry = null,
    Func<IConfiguration, CorrelationIdOptions>? correlationIdOptions = null) : IDependencyInjectionModule
{
    /// <inheritdoc />
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        services.UseModule(new ModuleCorrelationId(correlationIdOptions), configuration, environment);
        services.TryAddSingleton<IApplicationContext, ApplicationContext>();

        return coreTelemetry is null
            ? services.AddCoreTelemetry()
            : services.UseModule(coreTelemetry, configuration, environment);
    }
}
