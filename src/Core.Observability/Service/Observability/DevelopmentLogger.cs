using Anarchy.Toolkit.Core.Extension;
using Newtonsoft.Json;
using System.Collections;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

public enum DevelopmentLoggerLevel
{
    Debug,
    General, // Similar to info, but with no formatting
    Info,
    Warning,
    Error, // Default
    Off
}

public enum DevelopmentLoggerOutputMode
{
    Console,
    File
}

public interface IDevelopmentLogSink
{
    void WriteLine(string text);
}

internal sealed class ConsoleDevelopmentLogSink : IDevelopmentLogSink
{
    public void WriteLine(string text)
    {
        Console.Out.WriteLine(text);
    }
}

internal sealed class FileDevelopmentLogSink : IDevelopmentLogSink, IDisposable
{
    private readonly TextWriter _writer;
    private readonly object _sync = new();
    private bool _isDisposed;

    public FileDevelopmentLogSink(string filePath, bool append = true)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(filePath, append) { AutoFlush = true };
    }

    public void WriteLine(string text)
    {
        lock (_sync)
        {
            // A concurrent Print may still hold this sink after it has been replaced
            if (_isDisposed)
                return;

            _writer.WriteLine(text);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _writer.Dispose();
        }
    }
}

public static class DevelopmentLogger
{
    private const string EnvVarNameLevel = "DEV_LOG_LEVEL";
    private const string EnvVarNameOutputMode = "DEV_LOG_OUTPUT_MODE";
    private const string EnvVarNameOutputFilename = "DEV_LOG_OUTPUT_FILENAME";

    // ANSI escape codes for colors and formatting
    private const string AnsiReset = "\u001b[0m";
    private const string AnsiBold = "\u001b[1m";
    private const string AnsiMagenta = "\u001b[35m";
    private const string AnsiBlue = "\u001b[34m";
    private const string AnsiYellow = "\u001b[33m";
    private const string AnsiRed = "\u001b[31m";
    private const string AnsiGray = "\u001b[90m";

    private static readonly object SinkLock = new();

    private static IDevelopmentLogSink? _sink;
    private static bool _sinkInitialized;
    private static bool _ownsSink;

    public static DevelopmentLoggerLevel GetLogLevel(string? logLevel = null)
    {
        var levelString = logLevel ?? Environment.GetEnvironmentVariable(EnvVarNameLevel);

        if (!string.IsNullOrWhiteSpace(levelString) &&
            Enum.TryParse<DevelopmentLoggerLevel>(levelString, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return DevelopmentLoggerLevel.Error;
    }

    public static void SetSessionOutputMode(DevelopmentLoggerOutputMode outputMode, bool overrideCurrent = false)
    {
        if (overrideCurrent || Environment.GetEnvironmentVariable(EnvVarNameOutputMode) is null)
        {
            Environment.SetEnvironmentVariable(EnvVarNameOutputMode, outputMode.ToString());
        }
    }

    public static void SetSessionLogLevel(DevelopmentLoggerLevel logLevel, bool overrideCurrent = false)
    {
        if (overrideCurrent || Environment.GetEnvironmentVariable(EnvVarNameLevel) is null)
        {
            Environment.SetEnvironmentVariable(EnvVarNameLevel, logLevel.ToString());
        }
    }

    public static bool IsLevelOrHigher(
        DevelopmentLoggerLevel receivedLevel,
        string? minimumLevel = null)
    {
        var levels = (DevelopmentLoggerLevel[])Enum.GetValues(typeof(DevelopmentLoggerLevel));
        var min = GetLogLevel(minimumLevel);

        var receivedIndex = Array.IndexOf(levels, receivedLevel);
        var minimumIndex = Array.IndexOf(levels, min);

        return receivedIndex >= minimumIndex;
    }

    /// <summary>
    /// Programmatically override the sink. This takes precedence over environment variables.
    /// A sink created from environment variables is disposed when replaced; a sink provided here stays owned by the
    /// caller.
    /// </summary>
    public static void SetSink(IDevelopmentLogSink sink)
    {
        if (sink is null) throw new ArgumentNullException(nameof(sink));

        lock (SinkLock)
        {
            if (_ownsSink && !ReferenceEquals(_sink, sink))
            {
                (_sink as IDisposable)?.Dispose();
            }

            _sink = sink;
            _ownsSink = false;
            _sinkInitialized = true;
        }
    }

    public static void PrintEnvironment()
    {
        Print("Command Line Args", "DEVELOPMENT LOGGER", DevelopmentLoggerLevel.Debug,
            Environment.GetCommandLineArgs());

        Print("Environment Variables", "DEVELOPMENT LOGGER", DevelopmentLoggerLevel.Debug,
            new SortedList(Environment.GetEnvironmentVariables()));
    }

    public static void Print(
        string message,
        string? prefix = null,
        DevelopmentLoggerLevel messageLevel = DevelopmentLoggerLevel.General,
        object? context = null,
        bool spreadContext = false,
        string? minimumLevelOverride = null)
    {
        if (!IsLevelOrHigher(messageLevel, minimumLevelOverride))
            return;

        EnsureSinkInitialized();

        var prefixText = string.IsNullOrEmpty(prefix) ? "" : $" - {prefix}";
        var timestamp = DateTime.UtcNow.ToString("O");
        var header = $"{AnsiBold}[{timestamp}{prefixText}]{AnsiReset}";

        var coloredMessage = ApplyLevelColor(message, messageLevel);

        _sink!.WriteLine($"{header} {coloredMessage}");

        foreach (var ctxLine in FormatContextLines(context, spreadContext))
        {
            _sink.WriteLine($"{AnsiGray}{ctxLine}{AnsiReset}");
        }

        _sink.WriteLine(string.Empty);
    }

    // --------- private helpers ---------

    private static void EnsureSinkInitialized()
    {
        if (_sinkInitialized && _sink is not null)
            return;

        lock (SinkLock)
        {
            if (_sinkInitialized && _sink is not null)
                return;

            _sink = CreateSinkFromEnvironment();
            _ownsSink = true;
            _sinkInitialized = true;
        }
    }

    private static IDevelopmentLogSink CreateSinkFromEnvironment()
    {
        var mode = GetOutputModeFromEnvironment();

        return mode switch {
            DevelopmentLoggerOutputMode.File => new FileDevelopmentLogSink(
                GetOutputFilePathFromEnvironment()
            ),
            _ => new ConsoleDevelopmentLogSink()
        };
    }

    private static DevelopmentLoggerOutputMode GetOutputModeFromEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable(EnvVarNameOutputMode);

        if (string.IsNullOrWhiteSpace(raw))
            return DevelopmentLoggerOutputMode.Console;

        return Enum.TryParse<DevelopmentLoggerOutputMode>(raw, ignoreCase: true, out var parsed) ?
            parsed :
            DevelopmentLoggerOutputMode.Console;
    }

    private static string GetOutputFilePathFromEnvironment()
    {
        var explicitPath = Environment.GetEnvironmentVariable(EnvVarNameOutputFilename);
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return explicitPath;
        }

        // Default: <solution-root>/logs/runs-{yyyy-MM-dd}.log
        var root = Directory.GetCurrentDirectory();
        var logsDir = Path.Combine(root, "logs");

        var datePart = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var fileName = $"runs-{datePart}.log";

        return Path.Combine(logsDir, fileName);
    }

    private static string ApplyLevelColor(string message, DevelopmentLoggerLevel level)
    {
        if (GetOutputModeFromEnvironment() == DevelopmentLoggerOutputMode.File)
            return $"{level.ToString().ToUpperInvariant()}: {message}";

        return level switch {
            DevelopmentLoggerLevel.Debug => $"{AnsiMagenta}{message}{AnsiReset}",
            DevelopmentLoggerLevel.Info => $"{AnsiBlue}{message}{AnsiReset}",
            DevelopmentLoggerLevel.Warning => $"{AnsiYellow}{message}{AnsiReset}",
            DevelopmentLoggerLevel.Error => $"{AnsiRed}{message}{AnsiReset}",
            DevelopmentLoggerLevel.General => message,
            DevelopmentLoggerLevel.Off => message,
            _ => message
        };
    }

    private static IEnumerable<string> FormatContextLines(object? context, bool spreadContext)
    {
        switch (context)
        {
            case null:
                yield break;
            case string s:
                yield return s;

                yield break;
        }

        switch (spreadContext)
        {
            case true when context is IDictionary dict:
                {
                    foreach (DictionaryEntry entry in dict)
                    {
                        yield return $"{entry.Key}: {entry.Value?.ToJson()}";
                    }

                    yield break;
                }
            case true when context is IEnumerable<KeyValuePair<string, object?>> kvps:
                {
                    foreach (var kvp in kvps)
                    {
                        yield return $"{kvp.Key}: {kvp.Value?.ToJson()}";
                    }

                    yield break;
                }
            default:
                yield return context.ToJson(formatting: Formatting.Indented);

                break;
        }
    }
}
