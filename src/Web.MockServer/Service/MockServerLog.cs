using Anarchy.Toolkit.Web.MockServer.Entity;
using Microsoft.Extensions.Logging;

namespace Anarchy.Toolkit.Web.MockServer.Service;

/// <summary>
/// The mock server's log: the last <see cref="MockServerOptions.LogBuffer"/> entries, kept whatever the console
/// verbosity, with <see cref="EntryWritten"/> for live followers (e.g. the UI). Thread-safe.
/// </summary>
public sealed class MockServerLog
{
    private readonly Lock _lock = new();
    private readonly LinkedList<MockServerLogEntry> _entries = [];
    private readonly int _limit;
    private long _sequence;

    /// <summary>Creates a log keeping up to <paramref name="limit"/> entries (<c>0</c> keeps them all).</summary>
    /// <param name="limit">How many entries to keep.</param>
    public MockServerLog(int limit = 1000) => _limit = limit;

    /// <summary>Raised after an entry is written, outside the lock.</summary>
    public event Action<MockServerLogEntry>? EntryWritten;

    /// <summary>The kept entries, oldest first.</summary>
    public IReadOnlyList<MockServerLogEntry> All {
        get {
            lock (_lock) return _entries.ToList();
        }
    }

    /// <summary>The kept entries after <paramref name="sequence"/>, oldest first.</summary>
    /// <param name="sequence">The last sequence already seen.</param>
    /// <returns>The entries.</returns>
    public IReadOnlyList<MockServerLogEntry> After(long sequence)
    {
        lock (_lock) return _entries.Where(e => e.Sequence > sequence).ToList();
    }

    /// <summary>Writes an entry, assigning its sequence and, when unset, its timestamp.</summary>
    /// <param name="entry">The entry.</param>
    public void Write(MockServerLogEntry entry)
    {
        lock (_lock)
        {
            entry.Sequence = ++_sequence;
            if (entry.Timestamp == default) entry.Timestamp = DateTimeOffset.Now;
            _entries.AddLast(entry);
            if (_limit > 0 && _entries.Count > _limit) _entries.RemoveFirst();
        }
        EntryWritten?.Invoke(entry);
    }

    /// <summary>Writes a message.</summary>
    /// <param name="level">The level.</param>
    /// <param name="message">The message.</param>
    public void Write(MockServerLogLevel level, string message) => Write(new MockServerLogEntry { Level = level, Message = message });

    /// <summary>Forgets the kept entries (sequence numbers keep growing).</summary>
    /// <returns>How many were forgotten.</returns>
    public int Clear()
    {
        lock (_lock)
        {
            var count = _entries.Count;
            _entries.Clear();
            return count;
        }
    }

    internal sealed class Provider(MockServerLog log) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new Logger(log, categoryName);

        public void Dispose()
        {
        }
    }

    private sealed class Logger(MockServerLog log, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        // ASP.NET Core logs every request at Information; those are already request entries.
        public bool IsEnabled(LogLevel logLevel) =>
            logLevel != LogLevel.None
            && logLevel >= (category.StartsWith("Microsoft.", StringComparison.Ordinal) ? LogLevel.Warning : LogLevel.Information);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            if (exception is not null) message += Environment.NewLine + exception;
            log.Write(new MockServerLogEntry {
                Level = logLevel switch {
                    >= LogLevel.Error => MockServerLogLevel.Error,
                    LogLevel.Warning => MockServerLogLevel.Warning,
                    _ => MockServerLogLevel.Debug,
                },
                Category = category,
                Message = message,
            });
        }
    }
}
