using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>The telemetry primitives every toolkit package builds on: a Microsoft logger and a metric recorder.</summary>
/// <remarks>
/// <para>The default implementation is <see cref="CoreTelemetry"/>, registered by
/// <see cref="TelemetryServiceCollectionExtensions.AddCoreTelemetry"/>. Replace it through dependency injection when
/// the logger or recorder must come from elsewhere; implementations only supply the two primitives, the toolkit
/// conventions live in <see cref="AppTelemetry"/>.</para>
/// <para>Structured log scopes are opened with <see cref="ILogger.BeginScope{TState}"/>, or through
/// <see cref="AppTelemetry.LogContext"/>.</para>
/// </remarks>
public interface ICoreTelemetry
{
    /// <summary>The logger toolkit packages write to, created from the application's logging pipeline.</summary>
    ILogger Logger { get; }

    /// <summary>The recorder toolkit packages send metrics to.</summary>
    IMetricRecorder Metrics { get; }
}
