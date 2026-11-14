using AwesomeAssertions.Execution;
using Serilog.Events;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;

public static class LogEventExtensions
{
    public static LogEventAssertions Should(this LogEvent logEvent) => new(logEvent, AssertionChain.GetOrCreate());
}
