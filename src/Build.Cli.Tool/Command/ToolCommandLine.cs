using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Tool.Command;

public enum ToolMode
{
    Help,

    /// <summary>No command: scaffold inside an existing solution, otherwise bootstrap one.</summary>
    Auto,
    Bootstrap,
    Scaffold,
    Invalid
}

/// <summary>The parsed command line. Without a solution name (bootstrap) or application (scaffold), the wizard runs.</summary>
public sealed record ToolCommand(
    ToolMode Mode,
    string? SolutionName = null,
    string? OutputDirectory = null,
    ApplicationSpec? Application = null,
    string? Error = null)
{
    public bool IsInteractive => Mode == ToolMode.Auto ||
                                 (Mode == ToolMode.Bootstrap && SolutionName is null) ||
                                 (Mode == ToolMode.Scaffold && Application is null);
}

/// <summary>Parses <c>anarchy-toolkit [bootstrap|scaffold] [arguments]</c>.</summary>
public static class ToolCommandLine
{
    public const string BootstrapUsage = "anarchy-toolkit bootstrap [<solution-name> [--output <empty-directory>]]";
    public const string ScaffoldUsage = "anarchy-toolkit scaffold [api|job|ui <Name>]";

    public static ToolCommand Parse(IReadOnlyList<string> args) => args switch {
        [] => new ToolCommand(ToolMode.Auto),
        ["--help" or "-h" or "help", ..] => new ToolCommand(ToolMode.Help),
        ["bootstrap", ..] => ParseBootstrap(args.Skip(1).ToArray()),
        ["scaffold", ..] => ParseScaffold(args.Skip(1).ToArray()),
        [var command, ..] => Invalid($"Unknown command '{command}'.")
    };

    private static ToolCommand ParseBootstrap(IReadOnlyList<string> args) => args switch {
        [] => new ToolCommand(ToolMode.Bootstrap),
        [var name] when !name.StartsWith('-') => new ToolCommand(ToolMode.Bootstrap, name),
        [var name, "--output" or "-o", var output] when !name.StartsWith('-') && !string.IsNullOrWhiteSpace(output) =>
            new ToolCommand(ToolMode.Bootstrap, name, output),
        _ => Invalid("Usage: " + BootstrapUsage)
    };

    private static ToolCommand ParseScaffold(IReadOnlyList<string> args) => args switch {
        [] => new ToolCommand(ToolMode.Scaffold),
        [var kind, var name] when TryParseKind(kind, out var parsed) =>
            new ToolCommand(ToolMode.Scaffold, Application: new ApplicationSpec(parsed, name)),
        _ => Invalid("Usage: " + ScaffoldUsage)
    };

    private static bool TryParseKind(string value, out ApplicationKind kind) =>
        Enum.TryParse(value, ignoreCase: true, out kind) && Enum.IsDefined(kind);

    private static ToolCommand Invalid(string error) => new(ToolMode.Invalid, Error: error);
}
