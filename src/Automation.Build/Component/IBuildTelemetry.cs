using Anarchy.Toolkit.Automation.Build.Infrastructure.Telemetry;
using Anarchy.Toolkit.Core.Telemetry;
using Fallout.Common;
using Serilog;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Optional: sends the build's logs to the consumer's own Serilog sinks as well as to Fallout's output, and gives
/// build code the toolkit's <see cref="AppTelemetry"/> over that pipeline.
/// </summary>
/// <remarks>
/// <para>Fallout owns the build's logging: right after creating the build it replaces <see cref="Log.Logger"/> with its
/// console (themed, honouring <c>--verbosity</c>), CI annotation, warnings-summary and <c>build.log</c> sinks. The
/// <see cref="BuildTelemetryAttribute"/> on the build class wraps that logger once it is configured, adding the logger
/// built by <see cref="ConfigureBuildLogs"/>; Fallout's sinks are left untouched.</para>
/// <para>What stays engine-only: events logged before the build is created, the end-of-build "Errors &amp; Warnings"
/// replay and outcome tables (written to a console-only logger), and output written directly to the console (for
/// example the toolkit's tables and sections).</para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// [BuildTelemetry]
/// sealed class Build : AnarchyBuild, IStandardBuild, IBuildTelemetry
/// {
///     LoggerConfiguration? IBuildTelemetry.ConfigureBuildLogs(LoggerConfiguration configuration) =>
///         configuration.MinimumLevel.Information().WriteTo.File("logs/build.json", new JsonFormatter());
/// }
/// </code>
/// </example>
public interface IBuildTelemetry : IFalloutBuild
{
    /// <summary>
    /// Adds the consumer's sinks for the build logs. Return <see langword="null"/> (the default) to keep Fallout's
    /// output only.
    /// </summary>
    /// <param name="configuration">A configuration with no sinks; events arrive already enriched with <c>ExecutingTarget</c>.</param>
    /// <returns>The configured logger configuration, or <see langword="null"/>.</returns>
    LoggerConfiguration? ConfigureBuildLogs(LoggerConfiguration configuration) => null;

    /// <summary>
    /// The toolkit's telemetry over the build's logging pipeline (Fallout's sinks plus the consumer's). Metrics are
    /// discarded. Before the build is created it discards everything.
    /// </summary>
    AppTelemetry Telemetry => BuildTelemetryAttribute.Telemetry;
}
