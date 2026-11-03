using Serilog.Events;

namespace Anarchy.Toolkit.Tests.Unit.Testing.Observability.Monitoring.TestHelpers;

internal class TestLogEvent : LogEvent
{
    public TestLogEvent(
        DateTimeOffset? timestamp = null,
        LogEventLevel level = LogEventLevel.Information,
        Exception? exception = null,
        MessageTemplate? messageTemplate = null,
        params LogEventProperty[]? properties)
        : base(timestamp ?? DateTimeOffset.MinValue,
               level,
               exception,
               messageTemplate ?? MessageTemplate.Empty,
               properties ?? Enumerable.Empty<LogEventProperty>())
    {
    }

    /// <summary>
    /// Creates <paramref name="count"/> distinct log events (replaces the legacy AutoFixture <c>CreateMany</c>).
    /// </summary>
    public static IEnumerable<TestLogEvent> CreateMany(int count = 3) =>
        Enumerable.Range(0, count)
                  .Select(i => new TestLogEvent(
                              timestamp: DateTimeOffset.UtcNow.AddSeconds(i),
                              level: (LogEventLevel)(i % 6),
                              exception: new InvalidOperationException(TestData.AnyString())
                          ));
}
