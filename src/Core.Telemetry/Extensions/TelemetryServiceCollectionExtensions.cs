using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Registers the toolkit's telemetry.</summary>
/// <remarks>
/// <para><b>Precedence.</b> Every registration uses <c>TryAdd</c>, so whatever the application registered first wins:
/// an <see cref="IMetricRecorder"/>, an <see cref="ICoreTelemetry"/> or an <see cref="AppTelemetry"/> registered
/// before this call is kept. A registration added after this call also wins, because the container resolves the last
/// one; use <c>services.Replace(...)</c> to make that explicit.</para>
/// <para><b>Infrastructure.</b> Logs go to the application's <see cref="ILoggerFactory"/> (its providers decide the
/// destination) and metrics to its <see cref="IMeterFactory"/>. <c>AddLogging()</c> and <c>AddMetrics()</c> are
/// called, which only fill in what is missing.</para>
/// <para><b>Ownership.</b> The toolkit types never dispose the logger or recorder they receive. Externally created
/// instances registered with <c>AddSingleton(instance)</c> remain owned by the application; registrations made by
/// type or factory follow the container's normal disposal rules. The default logger is owned by its factory, and the
/// default meter is owned by the application's <see cref="IMeterFactory"/>.</para>
/// </remarks>
public static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IMetricRecorder"/> (<see cref="MeterMetricRecorder"/>), <see cref="ICoreTelemetry"/>
    /// (<see cref="CoreTelemetry"/>) and <see cref="AppTelemetry"/>, keeping any existing registration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddCoreTelemetry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddLogging();
        services.AddMetrics();
        services.TryAddSingleton<IMetricRecorder>(provider =>
            new MeterMetricRecorder(provider.GetRequiredService<IMeterFactory>()));
        services.TryAddSingleton<ICoreTelemetry>(provider => new CoreTelemetry(
            provider.GetRequiredService<ILoggerFactory>().CreateLogger(CoreTelemetry.InstrumentationName),
            provider.GetRequiredService<IMetricRecorder>()));
        services.TryAddSingleton<AppTelemetry>(provider => new AppTelemetry(provider.GetRequiredService<ICoreTelemetry>()));
        return services;
    }
}
