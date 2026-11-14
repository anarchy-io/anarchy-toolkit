using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Destructuring;
using Serilog;
using Serilog.Configuration;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;

public static class LoggerConfigurationExtensions
{
    public static LoggerConfiguration InMemory(this LoggerSinkConfiguration loggerConfiguration,
                                               out InMemorySink sink) =>
        loggerConfiguration
            .Sink(sink = new InMemorySink())
            .Destructure.With(new ObjectTestDestructuringPolicy());
}
