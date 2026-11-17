using Anarchy.Toolkit.Web.MockServer.Entity;
using Spectre.Console;

namespace Anarchy.Toolkit.Web.MockServer.Service;

/// <summary>
/// The mock server's console output, filtered by <see cref="MockServerOptions.Verbosity"/>: a startup banner, one
/// coloured line per request and, at higher verbosities, request and response details.
/// </summary>
public sealed class MockServerConsole
{
    /// <summary>The prefix of the line announcing the server is ready, always written to standard output.</summary>
    public const string ReadyPrefix = "MOCKSERVER_READY";

    private readonly IAnsiConsole _console;
    private readonly Lock _lock = new();

    /// <summary>Creates the console for <paramref name="options"/>.</summary>
    /// <param name="options">The server options.</param>
    /// <param name="console">The console to write to; standard output by default.</param>
    public MockServerConsole(MockServerOptions options, IAnsiConsole? console = null)
    {
        Verbosity = options.Verbosity;
        _console = console ?? AnsiConsole.Create(new AnsiConsoleSettings {
            Out = new AnsiConsoleOutput(Console.Out),
            ColorSystem = options.NoColor ? ColorSystemSupport.NoColors : ColorSystemSupport.Detect,
        });
    }

    /// <summary>The verbosity in use.</summary>
    public MockServerVerbosity Verbosity { get; }

    /// <summary>Whether output at <paramref name="level"/> is written.</summary>
    /// <param name="level">The level.</param>
    /// <returns><see langword="true"/> when <see cref="Verbosity"/> is at least <paramref name="level"/>.</returns>
    public bool IsEnabled(MockServerVerbosity level) => Verbosity >= level;

    /// <summary>Writes the startup banner (normal and above) or a one-line summary (minimal).</summary>
    /// <param name="url">The URL listened on.</param>
    /// <param name="options">The server options.</param>
    /// <param name="load">The loaded expectations.</param>
    /// <param name="codeExpectations">How many expectations were added in code.</param>
    /// <param name="middlewares">How many consumer middlewares are plugged in.</param>
    /// <param name="recordFile">The record file, if any.</param>
    public void Banner(string url, MockServerOptions options, ExpectationLoader.Result load, int codeExpectations, int middlewares, string? recordFile)
    {
        var total = load.Expectations.Count + codeExpectations;
        if (Verbosity == MockServerVerbosity.Minimal)
        {
            Write($"[bold green]Mock server[/] listening on [link]{Esc(url)}[/] with [white]{total}[/] expectation(s)");
        }
        else if (IsEnabled(MockServerVerbosity.Normal))
        {
            var grid = new Grid().AddColumn(new GridColumn().NoWrap()).AddColumn();
            grid.AddRow("[grey]Listening on[/]", $"[bold link]{Esc(url)}[/]");
            grid.AddRow("[grey]Expectations[/]", $"[white]{total}[/] ({load.Files.Count} file(s){(codeExpectations > 0 ? $", {codeExpectations} in code" : "")}){(options.WatchExpectations ? " [grey]· watching[/]" : "")}");
            foreach (var (file, count) in load.Files)
                grid.AddRow("", $"[grey]{Esc(MockServerJson.DisplayPath(file))}[/] [white]{count}[/]");
            grid.AddRow("[grey]Middlewares[/]", middlewares.ToString());
            grid.AddRow("[grey]Recording to[/]", recordFile is null ? "[grey]memory only[/]" : Esc(recordFile));
            grid.AddRow("[grey]Unmatched →[/]", options.UnmatchedStatusCode.ToString());
            if (options.KillTimeout is { } timeout) grid.AddRow("[grey]Stops after[/]", $"{timeout.TotalSeconds:0}s");
            grid.AddRow("[grey]Control API[/]", $"{Esc(url)}/__mock");
            grid.AddRow("[grey]Verbosity[/]", Verbosity.ToString().ToLowerInvariant());

            lock (_lock)
                _console.Write(new Panel(grid).Header("[bold green] Anarchy Mock Server [/]").Border(BoxBorder.Rounded).BorderColor(Color.Green));
        }

        foreach (var (source, error) in load.Errors) Warning($"{source}: {error}");
    }

    /// <summary>Writes the machine-readable ready line, whatever the verbosity.</summary>
    /// <param name="port">The port.</param>
    /// <param name="url">The URL.</param>
    public static void Ready(int port, string url)
    {
        Console.Out.WriteLine($"{ReadyPrefix} port={port} url={url}");
        Console.Out.Flush();
    }

    /// <summary>Writes one line for a handled request, plus details at higher verbosities.</summary>
    /// <param name="record">The recorded request.</param>
    /// <param name="control">Whether it was a <c>/__mock</c> control request (only shown at diagnostic).</param>
    /// <param name="responseBody">The response body, shown at diagnostic.</param>
    public void Request(RecordedRequest record, bool control, string? responseBody)
    {
        var unmatched = record.MatchedExpectation is null && !control;
        var level = control ? MockServerVerbosity.Diagnostic : unmatched ? MockServerVerbosity.Minimal : MockServerVerbosity.Normal;
        if (!IsEnabled(level)) return;

        var status = record.StatusCode switch {
            >= 500 => "red",
            >= 400 => "yellow",
            >= 300 => "cyan",
            _ => "green",
        };
        var method = record.Method.ToUpperInvariant() switch {
            "GET" => "blue",
            "POST" => "green",
            "PUT" or "PATCH" => "yellow",
            "DELETE" => "red",
            _ => "magenta",
        };
        var answer = control ? "[grey]control[/]"
            : unmatched ? "[yellow]no expectation matched[/]"
            : $"[grey]← {Esc(record.MatchedExpectation!)}[/]";

        var lines = new List<string> {
            $"[grey]{record.Timestamp:HH:mm:ss.fff}[/] [bold {method}]{Esc(record.Method),-6}[/] {Esc(record.Path + record.QueryString)} [bold {status}]{record.StatusCode}[/] [grey]{record.DurationMs:0}ms[/] {answer}",
        };

        if (IsEnabled(MockServerVerbosity.Detailed) && !control)
        {
            foreach (var (name, values) in record.Headers) lines.Add($"    [grey]{Esc(name)}:[/] {Esc(string.Join(", ", values))}");
            if (!string.IsNullOrEmpty(record.Body)) lines.Add($"    [grey]body:[/] {Esc(Truncate(record.Body))}");
        }
        if (IsEnabled(MockServerVerbosity.Diagnostic) && !string.IsNullOrEmpty(responseBody))
            lines.Add($"    [grey]response:[/] {Esc(Truncate(responseBody))}");

        lock (_lock)
            foreach (var line in lines) _console.MarkupLine(line);
    }

    /// <summary>Writes an informational message (normal and above).</summary>
    /// <param name="message">The message (plain text).</param>
    public void Info(string message)
    {
        if (IsEnabled(MockServerVerbosity.Normal)) Write($"[blue]ℹ[/] {Esc(message)}");
    }

    /// <summary>Writes a warning (minimal and above).</summary>
    /// <param name="message">The message (plain text).</param>
    public void Warning(string message)
    {
        if (IsEnabled(MockServerVerbosity.Minimal)) Write($"[yellow]⚠ {Esc(message)}[/]");
    }

    /// <summary>Writes an error (always).</summary>
    /// <param name="message">The message (plain text).</param>
    public void Error(string message) => Write($"[bold red]✖ {Esc(message)}[/]");

    private void Write(string markup)
    {
        lock (_lock) _console.MarkupLine(markup);
    }

    private static string Esc(string text) => Markup.Escape(text);

    private static string Truncate(string text) => text.Length <= 2000 ? text.ReplaceLineEndings(" ") : text[..2000].ReplaceLineEndings(" ") + "…";
}
