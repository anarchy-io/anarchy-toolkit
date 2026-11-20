using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Anarchy.Toolkit.Testing.Telemetry;

/// <summary>
/// A Microsoft logging provider that keeps every log event in memory, with its structured properties and the
/// properties of the scopes open when it was written, so tests can assert on them.
/// </summary>
/// <remarks>Captures every level. Thread-safe. Disposing the provider does not discard the captured events.</remarks>
public sealed class InMemoryLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentQueue<RecordedLog> _logs = new();
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

    /// <summary>Every log event captured so far, in order.</summary>
    public IReadOnlyList<RecordedLog> Logs => [.. _logs];

    /// <summary>Discards the captured events.</summary>
    public void Clear() => _logs.Clear();

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new InMemoryLogger(this, categoryName);

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    /// <inheritdoc />
    public void Dispose() { }

    private sealed class InMemoryLogger(InMemoryLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => provider._scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
            string? template = null;
            if (state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                foreach (var (key, value) in values)
                {
                    if (key == "{OriginalFormat}") template = value as string;
                    else properties[key] = value;
                }
            }

            var scopes = new Dictionary<string, object?>(StringComparer.Ordinal);
            provider._scopes.ForEachScope((scope, target) => {
                if (scope is IEnumerable<KeyValuePair<string, object?>> scopeValues)
                    foreach (var (key, value) in scopeValues)
                        target[key] = value;
            }, scopes);

            provider._logs.Enqueue(new RecordedLog(category, logLevel, eventId, formatter(state, exception), template,
                properties, scopes, exception));
        }
    }
}
