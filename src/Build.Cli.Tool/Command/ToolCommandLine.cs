using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Tool.Command;

/// <summary>
/// Parses <c>anarchy-toolkit [bootstrap|scaffold] [arguments]</c>.
/// </summary>
public static class ToolCommandLine
{
    public const string BootstrapUsage = "anarchy-toolkit bootstrap [<solution-name> [--output <empty-directory>]]";
    public const string ScaffoldUsage = "anarchy-toolkit scaffold [api|job|ui <Name>]";

    /// <summary>
    /// Parses the arguments, without the executable name. Never throws: malformed input yields a
    /// <see cref="ToolMode.Invalid"/> command carrying the error.
    /// </summary>
    public static ToolCommand Parse(IReadOnlyList<string> args) => args switch {
        [] or ["--help" or "-h" or "help", ..] => new ToolCommand(ToolMode.Help),
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
