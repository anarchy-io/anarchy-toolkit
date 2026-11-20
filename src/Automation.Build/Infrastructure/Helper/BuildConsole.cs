using Fallout.Common.Tools.DotNet;
using Spectre.Console;

namespace Anarchy.Toolkit.Automation.Build.Infrastructure.Helper;

/// <summary>
/// Shared Spectre.Console formatting for target output: section rules, status lines and tables. Errors and warnings
/// that must reach Fallout's summary keep going through Serilog.
/// </summary>
internal static class BuildConsole
{
    /// <summary>
    /// Width used when output is redirected (CI logs), where Spectre would otherwise fall back to 80 columns.
    /// </summary>
    private const int RedirectedWidth = 160;

    private static readonly Lazy<IAnsiConsole> DefaultConsole = new(() => {
        var console = AnsiConsole.Create(new AnsiConsoleSettings());
        if (Console.IsOutputRedirected) console.Profile.Width = Math.Max(console.Profile.Width, RedirectedWidth);
        return console;
    });

    /// <summary>
    /// The console written to. Defaults to the process console, widened when output is redirected.
    /// </summary>
    internal static IAnsiConsole Out => DefaultConsole.Value;

    /// <summary>
    /// Writes a blank line followed by a left-aligned rule titled <paramref name="title"/>.
    /// </summary>
    internal static void Section(string title, DotNetVerbosity? verbosity = null)
    {
        if (DotNetVerbosity.quiet.Equals(verbosity)) return;
        Out.WriteLine();
        Out.Write(new Rule($"[blue bold]{Markup.Escape(title)}[/]").LeftJustified());
        Out.WriteLine();
    }

    /// <summary>
    /// Writes <paramref name="message"/> in yellow after a blank line, for steps that were skipped.
    /// </summary>
    internal static void Skipped(string message, DotNetVerbosity? verbosity = null)
    {
        if (DotNetVerbosity.quiet.Equals(verbosity)) return;
        Out.WriteLine();
        Out.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Writes <paramref name="message"/> in green after a blank line, for steps that completed.
    /// </summary>
    internal static void Success(string message, DotNetVerbosity? verbosity = null)
    {
        if (DotNetVerbosity.quiet.Equals(verbosity)) return;
        Out.WriteLine();
        Out.MarkupLine($"[green]{Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Asks a yes/no <paramref name="question"/> after a blank line, defaulting to no.
    /// </summary>
    internal static bool Confirm(string question)
    {
        Out.WriteLine();
        Out.Markup($"[yellow]{Markup.Escape(question)}[/] [grey]{Markup.Escape("[y/N]")}[/] ");
        return string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a rounded table with bold <paramref name="columns"/>.
    /// </summary>
    internal static Table Table(params string[] columns)
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Grey);
        foreach (var column in columns) table.AddColumn($"[bold]{Markup.Escape(column)}[/]");
        return table;
    }

    /// <summary>
    /// Writes <paramref name="table"/> after a blank line.
    /// </summary>
    internal static void Write(Table table, DotNetVerbosity? verbosity = null)
    {
        if (DotNetVerbosity.quiet.Equals(verbosity)) return;
        Out.WriteLine();
        Out.Write(table);
    }
}
