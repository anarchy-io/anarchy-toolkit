using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Testing.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Testing.Extensions.Microsoft.Extensions.DependencyInjection;

/// <summary>Registers a <see cref="TestTelemetry"/> capture in a service collection.</summary>
public static class TestTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds the capture's logging provider, replaces the <see cref="IMetricRecorder"/> with its recorder and adds the
    /// toolkit telemetry, so everything resolved from the container reports to <paramref name="telemetry"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="telemetry">The capture; it stays owned by the caller.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddTestTelemetry(this IServiceCollection services, TestTelemetry telemetry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(telemetry);
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
        services.AddSingleton<ILoggerProvider>(telemetry.LoggerProvider);
        services.Replace(ServiceDescriptor.Singleton<IMetricRecorder>(telemetry.Metrics));
        return services.AddCoreTelemetry();
    }
}
