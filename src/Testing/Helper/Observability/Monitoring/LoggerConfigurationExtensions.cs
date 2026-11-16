using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Destructuring;
using Serilog;
using Serilog.Configuration;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;

/// <summary>
/// Serilog configuration extensions for in-memory test logging.
/// </summary>
public static class LoggerConfigurationExtensions
{
    /// <summary>
    /// Writes log events to a new <see cref="InMemorySink"/> and registers <see cref="ObjectTestDestructuringPolicy"/>,
    /// so destructured objects are kept as scalar values.
    /// </summary>
    /// <param name="loggerConfiguration">The sink configuration.</param>
    /// <param name="sink">Receives the created sink.</param>
    /// <returns>The logger configuration, for chaining.</returns>
    public static LoggerConfiguration InMemory(this LoggerSinkConfiguration loggerConfiguration,
                                               out InMemorySink sink) =>
        loggerConfiguration
            .Sink(sink = new InMemorySink())
            .Destructure.With(new ObjectTestDestructuringPolicy());
}
