using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Anarchy.Toolkit.Infrastructure.Telemetry.Serilog.Extensions.Microsoft.Extensions.DependencyInjection;

/// <summary>Registers Serilog as an optional logging provider for the toolkit's telemetry.</summary>
/// <remarks>
/// Serilog is added as one more Microsoft logging provider: other providers are not cleared, and the toolkit's
/// instrumentation code does not change. Metrics keep using the application's <see cref="IMetricRecorder"/>.
/// </remarks>
public static class SerilogTelemetryExtensions
{
    /// <summary>
    /// Adds an application-owned Serilog logger as a logging provider, then
    /// <see cref="TelemetryServiceCollectionExtensions.AddCoreTelemetry"/>.
    /// </summary>
    /// <remarks>The logger, its sinks and enrichers stay owned by the application: they are never disposed here.
    /// Adding the same logger twice registers it once.</remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="logger">The configured Serilog logger.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddSerilogTelemetry(this IServiceCollection services, global::Serilog.ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(SerilogLoggerRegistration)
                                        && descriptor.ImplementationInstance is SerilogLoggerRegistration registration
                                        && ReferenceEquals(registration.Logger, logger)))
        {
            services.AddLogging();
            services.AddSingleton(new SerilogLoggerRegistration(logger));
            services.AddSingleton<ILoggerProvider>(new SerilogLoggerProvider(logger, dispose: false));
        }

        return services.AddCoreTelemetry();
    }

    /// <summary>
    /// Builds a Serilog logger owned by the service provider (disposed, and so flushed, with it) and adds it as a
    /// logging provider, then <see cref="TelemetryServiceCollectionExtensions.AddCoreTelemetry"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the logger: sinks, enrichers, levels.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddSerilogTelemetry(this IServiceCollection services,
                                                         Action<LoggerConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new LoggerConfiguration();
        configure(configuration);
        services.AddLogging();
        services.AddSingleton<ILoggerProvider>(_ => new SerilogLoggerProvider(configuration.CreateLogger(), dispose: true));
        return services.AddCoreTelemetry();
    }

    /// <summary>
    /// Opt-in: also writes every metric as a log event through the registered logging providers, by decorating the
    /// registered <see cref="IMetricRecorder"/> (or the default one) with a <see cref="LogMetricRecorder"/>.
    /// </summary>
    /// <remarks>
    /// Call it after registering a custom recorder: a recorder registered afterwards replaces the decorator. Calling it
    /// again has no effect. Metric events use the <see cref="LogMetricRecorder.LoggerCategory"/> category, so they can
    /// be filtered separately.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="level">The level of the metric log events.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddTelemetryLogMetrics(this IServiceCollection services,
                                                            LogLevel level = LogLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(LogMetricsMarker)))
            return services;

        services.AddSingleton<LogMetricsMarker>();
        services.AddLogging();
        services.AddMetrics();

        var existing = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IMetricRecorder)
                                                            && !descriptor.IsKeyedService);
        Func<IServiceProvider, IMetricRecorder> inner = existing is null
            ? provider => new MeterMetricRecorder(provider.GetRequiredService<IMeterFactory>())
            : provider => Resolve(provider, existing);
        if (existing is not null) services.Remove(existing);

        services.Add(ServiceDescriptor.Describe(typeof(IMetricRecorder), provider => new LogMetricRecorder(
                inner(provider),
                provider.GetRequiredService<ILoggerFactory>().CreateLogger(LogMetricRecorder.LoggerCategory),
                level),
            existing?.Lifetime ?? ServiceLifetime.Singleton));

        return services.AddCoreTelemetry();
    }

    private static IMetricRecorder Resolve(IServiceProvider provider, ServiceDescriptor descriptor) =>
        descriptor.ImplementationInstance as IMetricRecorder
        ?? descriptor.ImplementationFactory?.Invoke(provider) as IMetricRecorder
        ?? (IMetricRecorder)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);

    private sealed class LogMetricsMarker;

    private sealed record SerilogLoggerRegistration(global::Serilog.ILogger Logger);
}
