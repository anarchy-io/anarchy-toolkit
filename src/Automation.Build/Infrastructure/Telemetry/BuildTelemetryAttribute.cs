using Anarchy.Toolkit.Automation.Build.Component;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;
using Fallout.Common.Execution;
using Serilog;
using Serilog.Core;

namespace Anarchy.Toolkit.Automation.Build.Infrastructure.Telemetry;

/// <summary>
/// Bridges Fallout's logging to the consumer's sinks (see <see cref="IBuildTelemetry"/>) and exposes the toolkit's
/// telemetry over it.
/// </summary>
/// <remarks>
/// <para>Runs when the build is created, after Fallout has configured <see cref="Log.Logger"/>: the Fallout logger is
/// kept as-is and wrapped together with the consumer's logger, so everything written through <see cref="Log"/>
/// (targets, toolkit components, tool invocations) reaches both.</para>
/// <para>The consumer's logger is created and owned here: it is disposed, and so flushed, when the build finishes,
/// after Fallout's summary. Fallout's own logger stays owned by Fallout. One build runs per process, so the telemetry
/// is process-wide.</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BuildTelemetryAttribute : BuildExtensionAttributeBase, IOnBuildCreated, IOnBuildFinished
{
    private Logger? _consumerLogger;

    /// <summary>The toolkit's telemetry over the current build's logging pipeline.</summary>
    public static AppTelemetry Telemetry { get; private set; } = AppTelemetry.Null;

    /// <summary>Runs before Fallout's own extensions so they log through the bridge.</summary>
    public override float Priority { get; set; } = 100;

    /// <inheritdoc />
    public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets) =>
        Attach(Build is IBuildTelemetry component ? component.ConfigureBuildLogs : null);

    internal void Attach(Func<LoggerConfiguration, LoggerConfiguration?>? configureBuildLogs)
    {
        var engineLogger = Log.Logger;
        var consumer = configureBuildLogs?.Invoke(new LoggerConfiguration());

        if (consumer is not null)
        {
            _consumerLogger = consumer.CreateLogger();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .ConfigureEnricher()
                .WriteTo.Logger(engineLogger)
                .WriteTo.Logger(_consumerLogger)
                .CreateLogger();
        }

        Telemetry = SerilogTelemetry.Create(Log.Logger);
    }

    /// <inheritdoc />
    public void OnBuildFinished()
    {
        Telemetry = AppTelemetry.Null;
        _consumerLogger?.Dispose();
        _consumerLogger = null;
    }
}
