using AwesomeAssertions.Execution;
using Serilog.Events;

namespace Anarchy.Toolkit.Testing.Helper.Observability.Monitoring.Assertions;

/// <summary>
/// Entry point extensions for asserting on a Serilog <see cref="LogEvent"/>.
/// </summary>
public static class LogEventExtensions
{
    /// <summary>
    /// Returns a <see cref="LogEventAssertions"/> object that can be used to assert on the log event's properties.
    /// </summary>
    /// <param name="logEvent">The log event under test.</param>
    /// <returns>A <see cref="LogEventAssertions"/> bound to the current <see cref="AssertionChain"/>.</returns>
    public static LogEventAssertions Should(this LogEvent logEvent) => new(logEvent, AssertionChain.GetOrCreate());
}
