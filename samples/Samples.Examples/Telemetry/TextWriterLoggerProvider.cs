using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Samples.Examples.Telemetry;

/// <summary>A minimal logging provider that writes one line per event, so the examples need no console package.</summary>
public sealed class TextWriterLoggerProvider(TextWriter writer) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new Logger(writer, categoryName);

    public void Dispose()
    {
    }

    private sealed class Logger(TextWriter writer, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                                Func<TState, Exception?, string> formatter) =>
            writer.WriteLine($"[{logLevel}] {category}: {formatter(state, exception)}");
    }
}
