using Anarchy.Toolkit.Web.MockServer.Entity;
using System.Globalization;

namespace Anarchy.Toolkit.Web.MockServer.Service;

/// <summary>
/// Reads <see cref="MockServerOptions"/> from command-line arguments, falling back to <c>MOCKSERVER_*</c> environment
/// variables. Options accept <c>--name value</c> and <c>--name=value</c>.
/// </summary>
public static class MockServerArguments
{
    /// <summary>The help text printed by <c>--help</c>.</summary>
    public const string Usage = """
        Usage: mockserver [options]

          -p, --port <port>            Port to listen on (default 6000, or the next free one)      MOCKSERVER_PORT
              --host <host>            Host to listen on (default localhost)                       MOCKSERVER_HOST
          -e, --expectations <path>    Expectation file, directory or glob; repeatable             MOCKSERVER_EXPECTATIONS (;-separated)
          -w, --watch                  Reload expectations when their files change                 MOCKSERVER_WATCH
          -r, --record <file>          Append every request to <file> as JSON lines                MOCKSERVER_RECORD
              --record-limit <n>       Requests kept in memory for /__mock/requests (0 = all)      MOCKSERVER_RECORD_LIMIT
              --kill-timeout <secs>    Stop after this many seconds                                MOCKSERVER_KILL_TIMEOUT
              --unmatched-status <n>   Status answered when nothing matches (default 404)          MOCKSERVER_UNMATCHED_STATUS
          -v, --verbosity <level>      q[uiet], m[inimal], n[ormal], d[etailed] or diag[nostic]    MOCKSERVER_VERBOSITY
          -q, --quiet                  Same as --verbosity quiet
              --no-color               Disable colours (also NO_COLOR)
          -h, --help                   Show this help
        """;

    /// <summary>The result of parsing.</summary>
    /// <param name="Options">The options.</param>
    /// <param name="ShowHelp">Whether <c>--help</c> was given.</param>
    /// <param name="Errors">Invalid or unknown arguments.</param>
    public sealed record ParseResult(MockServerOptions Options, bool ShowHelp, IReadOnlyList<string> Errors);

    /// <summary>
    /// Parses <paramref name="args"/>; unknown arguments are reported unless <paramref name="allowUnknown"/> is set.
    /// </summary>
    /// <param name="args">The process arguments.</param>
    /// <param name="environment">Reads environment variables; <see cref="Environment.GetEnvironmentVariable(string)"/> by default.</param>
    /// <param name="allowUnknown">Ignores unknown arguments (e.g. ones meant for a consumer's own host).</param>
    /// <returns>The options, whether help was asked for, and any errors.</returns>
    public static ParseResult Parse(string[] args, Func<string, string?>? environment = null, bool allowUnknown = false)
    {
        environment ??= Environment.GetEnvironmentVariable;
        var options = new MockServerOptions();
        var errors = new List<string>();
        var help = false;

        ApplyEnvironment(options, environment, errors);
        var expectationsFromArgs = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string? inline = null;
            if (arg.StartsWith("--") && arg.IndexOf('=') is var eq and > 0)
            {
                inline = arg[(eq + 1)..];
                arg = arg[..eq];
            }

            string? Value()
            {
                if (inline is not null) return inline;
                if (i + 1 < args.Length) return args[++i];
                errors.Add($"{arg} needs a value.");
                return null;
            }

            switch (arg)
            {
                case "-p" or "--port": SetInt(Value(), arg, v => options.Port = v, errors, 1, 65535); break;
                case "--host": if (Value() is { } host) options.Host = host; break;
                case "-e" or "--expectations": if (Value() is { } path) expectationsFromArgs.Add(path); break;
                case "-w" or "--watch": options.WatchExpectations = true; break;
                case "-r" or "--record": if (Value() is { } file) options.RecordFile = file; break;
                case "--record-limit": SetInt(Value(), arg, v => options.RecordLimit = v, errors, 0); break;
                case "--kill-timeout": SetInt(Value(), arg, v => options.KillTimeout = v > 0 ? TimeSpan.FromSeconds(v) : null, errors, 0); break;
                case "--unmatched-status": SetInt(Value(), arg, v => options.UnmatchedStatusCode = v, errors, 100, 599); break;
                case "-v" or "--verbosity": SetVerbosity(Value(), options, errors); break;
                case "-q" or "--quiet": options.Verbosity = MockServerVerbosity.Quiet; break;
                case "--no-color": options.NoColor = true; break;
                case "-h" or "--help" or "-?": help = true; break;
                default:
                    if (!allowUnknown) errors.Add($"Unknown argument '{args[i]}'.");
                    break;
            }
        }

        if (expectationsFromArgs.Count > 0)
        {
            options.ExpectationPaths.Clear();
            options.ExpectationPaths.AddRange(expectationsFromArgs);
        }

        return new ParseResult(options, help, errors);
    }

    /// <summary>Parses a verbosity name or its abbreviation (<c>q</c>, <c>m</c>, <c>n</c>, <c>d</c>, <c>diag</c>).</summary>
    /// <param name="value">The text.</param>
    /// <param name="verbosity">The verbosity.</param>
    /// <returns>Whether it was recognised.</returns>
    public static bool TryParseVerbosity(string? value, out MockServerVerbosity verbosity)
    {
        MockServerVerbosity? parsed = value?.Trim().ToLowerInvariant() switch {
            "q" or "quiet" => MockServerVerbosity.Quiet,
            "m" or "minimal" => MockServerVerbosity.Minimal,
            "n" or "normal" => MockServerVerbosity.Normal,
            "d" or "detailed" => MockServerVerbosity.Detailed,
            "diag" or "diagnostic" => MockServerVerbosity.Diagnostic,
            _ => null,
        };
        verbosity = parsed ?? MockServerVerbosity.Normal;
        return parsed is not null;
    }

    private static void ApplyEnvironment(MockServerOptions options, Func<string, string?> environment, List<string> errors)
    {
        SetInt(environment("MOCKSERVER_PORT"), "MOCKSERVER_PORT", v => options.Port = v, errors, 1, 65535);
        if (environment("MOCKSERVER_HOST") is { Length: > 0 } host) options.Host = host;
        if (environment("MOCKSERVER_EXPECTATIONS") is { Length: > 0 } paths)
            options.ExpectationPaths.AddRange(paths.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (IsTrue(environment("MOCKSERVER_WATCH"))) options.WatchExpectations = true;
        if (environment("MOCKSERVER_RECORD") is { Length: > 0 } record) options.RecordFile = record;
        SetInt(environment("MOCKSERVER_RECORD_LIMIT"), "MOCKSERVER_RECORD_LIMIT", v => options.RecordLimit = v, errors, 0);
        SetInt(environment("MOCKSERVER_KILL_TIMEOUT"), "MOCKSERVER_KILL_TIMEOUT", v => options.KillTimeout = v > 0 ? TimeSpan.FromSeconds(v) : null, errors, 0);
        SetInt(environment("MOCKSERVER_UNMATCHED_STATUS"), "MOCKSERVER_UNMATCHED_STATUS", v => options.UnmatchedStatusCode = v, errors, 100, 599);
        if (environment("MOCKSERVER_VERBOSITY") is { Length: > 0 } verbosity) SetVerbosity(verbosity, options, errors);
        if (environment("NO_COLOR") is { Length: > 0 }) options.NoColor = true;
    }

    private static bool IsTrue(string? value) => value is "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static void SetVerbosity(string? value, MockServerOptions options, List<string> errors)
    {
        if (value is null) return;
        if (TryParseVerbosity(value, out var verbosity)) options.Verbosity = verbosity;
        else errors.Add($"Unknown verbosity '{value}'.");
    }

    private static void SetInt(string? value, string name, Action<int> set, List<string> errors, int min, int max = int.MaxValue)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number >= min && number <= max)
            set(number);
        else
            errors.Add($"{name} must be a number between {min} and {max}, got '{value}'.");
    }
}
