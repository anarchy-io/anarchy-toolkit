using Anarchy.Toolkit.Core.Composition.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>
/// Base for an application's telemetry module: register the logging providers, recorder or primitives in
/// <see cref="RegisterCoreTelemetry"/>, and this base completes what the toolkit modules expect.
/// </summary>
/// <remarks>
/// <see cref="RegisterServices"/> calls <see cref="RegisterCoreTelemetry"/> first, then
/// <see cref="TelemetryServiceCollectionExtensions.AddCoreTelemetry"/>, which keeps the registrations made there and
/// only fills in the missing ones.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public sealed class CompanyTelemetryModule : ModuleTelemetryBase
/// {
///     protected override void RegisterCoreTelemetry(IServiceCollection services, IConfiguration configuration,
///                                                   IHostEnvironment environment)
///     {
///         services.AddLogging(logging => logging.AddJsonConsole());
///         services.AddSingleton&lt;IMetricRecorder, CompanyMetricRecorder&gt;();
///     }
/// }
/// </code>
/// </example>
public abstract class ModuleTelemetryBase : IDependencyInjectionModule
{
    /// <inheritdoc />
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        RegisterCoreTelemetry(services, configuration, environment);
        return services.AddCoreTelemetry();
    }

    /// <summary>
    /// Registers the application's logging providers and, optionally, its own <see cref="IMetricRecorder"/> or
    /// <see cref="ICoreTelemetry"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The hosting environment.</param>
    protected abstract void RegisterCoreTelemetry(IServiceCollection services, IConfiguration configuration,
                                                  IHostEnvironment environment);
}
