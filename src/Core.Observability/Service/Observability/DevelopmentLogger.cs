using Anarchy.Toolkit.Core.Extension;
using Newtonsoft.Json;
using System.Collections;

namespace Anarchy.Toolkit.Core.Observability.Service.Observability;

/// <summary>
/// Severity levels used by <see cref="DevelopmentLogger"/>, ordered from most to least verbose.
/// </summary>
/// <remarks>
/// A message is printed when its level is greater than or equal to the configured minimum level
/// (see <see cref="DevelopmentLogger.IsLevelOrHigher"/>). The default minimum level is <see cref="Error"/>.
/// </remarks>
public enum DevelopmentLoggerLevel
{
    /// <summary>Verbose diagnostic output, rendered in magenta on the console.</summary>
    Debug,

    /// <summary>General output, similar to <see cref="Info"/> but printed without color formatting.</summary>
    General, // Similar to info, but with no formatting

    /// <summary>Informational output, rendered in blue on the console.</summary>
    Info,

    /// <summary>Warnings, rendered in yellow on the console.</summary>
    Warning,

    /// <summary>Errors, rendered in red on the console. This is the default minimum level.</summary>
    Error, // Default

    /// <summary>Used as a minimum level to disable all output.</summary>
    Off
}

/// <summary>
/// Where <see cref="DevelopmentLogger"/> writes its output when no sink was set via
/// <see cref="DevelopmentLogger.SetSink"/>.
/// </summary>
public enum DevelopmentLoggerOutputMode
{
    /// <summary>Writes to standard output with ANSI colors. This is the default.</summary>
    Console,

    /// <summary>
    /// Appends to a file (path from <c>DEV_LOG_OUTPUT_FILENAME</c>, or <c>logs/runs-{yyyy-MM-dd}.log</c> under the
    /// current directory). Messages are prefixed with the level name instead of colored.
    /// </summary>
    File
}

/// <summary>
/// Destination for <see cref="DevelopmentLogger"/> output. Implement it to redirect output, e.g. in tests, and register
/// it with <see cref="DevelopmentLogger.SetSink"/>.
/// </summary>
public interface IDevelopmentLogSink
{
    /// <summary>
    /// Writes a single line of text. May be called concurrently from multiple threads.
    /// </summary>
    /// <param name="text">The text to write (may contain ANSI escape codes).</param>
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

/// <summary>
/// Lightweight, dependency-free logger meant for local development and early bootstrap diagnostics (e.g. before the
/// Serilog pipeline of <see cref="Observer"/> is configured).
/// </summary>
/// <remarks>
/// <para>Behavior is controlled by environment variables, read on each call unless stated otherwise:</para>
/// <list type="bullet">
/// <item><c>DEV_LOG_LEVEL</c>: minimum <see cref="DevelopmentLoggerLevel"/> (case-insensitive, default
/// <see cref="DevelopmentLoggerLevel.Error"/>).</item>
/// <item><c>DEV_LOG_OUTPUT_MODE</c>: <see cref="DevelopmentLoggerOutputMode"/> (default
/// <see cref="DevelopmentLoggerOutputMode.Console"/>). Read once when the sink is first created.</item>
/// <item><c>DEV_LOG_OUTPUT_FILENAME</c>: output file path for file mode (default
/// <c>logs/runs-{yyyy-MM-dd}.log</c> under the current directory).</item>
/// </list>
/// <para>The sink is created lazily and thread-safely on the first printed message; use <see cref="SetSink"/> to
/// override it programmatically.</para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// DevelopmentLogger.SetSessionLogLevel(DevelopmentLoggerLevel.Debug);
/// DevelopmentLogger.Print("Loaded settings", "BOOTSTRAP", DevelopmentLoggerLevel.Info, context: settings);
/// </code>
/// </example>
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

    /// <summary>
    /// Resolves the minimum log level.
    /// </summary>
    /// <param name="logLevel">
    /// An explicit level name (case-insensitive). When <see langword="null"/>, the <c>DEV_LOG_LEVEL</c> environment
    /// variable is used.
    /// </param>
    /// <returns>The parsed level, or <see cref="DevelopmentLoggerLevel.Error"/> when missing or invalid.</returns>
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

    /// <summary>
    /// Sets the <c>DEV_LOG_OUTPUT_MODE</c> environment variable for the current process.
    /// </summary>
    /// <remarks>
    /// Only affects the sink if called before the first message is printed (the sink is created once) and no sink was
    /// set via <see cref="SetSink"/>.
    /// </remarks>
    /// <param name="outputMode">The output mode to use.</param>
    /// <param name="overrideCurrent">
    /// When <see langword="false"/> (default), the variable is only set if it is not already defined.
    /// </param>
    public static void SetSessionOutputMode(DevelopmentLoggerOutputMode outputMode, bool overrideCurrent = false)
    {
        if (overrideCurrent || Environment.GetEnvironmentVariable(EnvVarNameOutputMode) is null)
        {
            Environment.SetEnvironmentVariable(EnvVarNameOutputMode, outputMode.ToString());
        }
    }

    /// <summary>
    /// Sets the <c>DEV_LOG_LEVEL</c> environment variable for the current process.
    /// </summary>
    /// <param name="logLevel">The minimum level to print.</param>
    /// <param name="overrideCurrent">
    /// When <see langword="false"/> (default), the variable is only set if it is not already defined.
    /// </param>
    public static void SetSessionLogLevel(DevelopmentLoggerLevel logLevel, bool overrideCurrent = false)
    {
        if (overrideCurrent || Environment.GetEnvironmentVariable(EnvVarNameLevel) is null)
        {
            Environment.SetEnvironmentVariable(EnvVarNameLevel, logLevel.ToString());
        }
    }

    /// <summary>
    /// Determines whether a message of <paramref name="receivedLevel"/> passes the minimum level.
    /// </summary>
    /// <param name="receivedLevel">The level of the message.</param>
    /// <param name="minimumLevel">
    /// An explicit minimum level name; when <see langword="null"/>, resolved via <see cref="GetLogLevel"/>.
    /// </param>
    /// <returns><see langword="true"/> if the message should be printed.</returns>
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
    /// <param name="sink">The sink that receives all subsequent output.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sink"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Prints the command line arguments and all environment variables (sorted) at
    /// <see cref="DevelopmentLoggerLevel.Debug"/> level.
    /// </summary>
    /// <remarks>Environment variables may contain secrets; use only in local development.</remarks>
    public static void PrintEnvironment()
    {
        Print("Command Line Args", "DEVELOPMENT LOGGER", DevelopmentLoggerLevel.Debug,
            Environment.GetCommandLineArgs());

        Print("Environment Variables", "DEVELOPMENT LOGGER", DevelopmentLoggerLevel.Debug,
            new SortedList(Environment.GetEnvironmentVariables()));
    }

    /// <summary>
    /// Prints a message (with UTC timestamp and optional prefix header) followed by an optional context block and a
    /// blank line, if <paramref name="messageLevel"/> passes the minimum level.
    /// </summary>
    /// <param name="message">The message to print.</param>
    /// <param name="prefix">Optional text appended to the timestamp header.</param>
    /// <param name="messageLevel">The message level. Defaults to <see cref="DevelopmentLoggerLevel.General"/>.</param>
    /// <param name="context">
    /// Optional context. Strings are printed as-is; other objects are serialized to indented JSON.
    /// </param>
    /// <param name="spreadContext">
    /// When <see langword="true"/> and <paramref name="context"/> is a dictionary or a sequence of key/value pairs,
    /// prints one <c>key: json-value</c> line per entry instead of a single JSON document.
    /// </param>
    /// <param name="minimumLevelOverride">
    /// Optional minimum level name that overrides <c>DEV_LOG_LEVEL</c> for this call.
    /// </param>
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
