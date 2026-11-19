using Anarchy.Toolkit.Automation.Core.Entity;

namespace Anarchy.Toolkit.Automation.Build.Cli.Command;

/// <summary>
/// Parses <c>anarchy-toolkit [bootstrap|scaffold|skills|boilerplate] [arguments]</c>.
/// </summary>
/// <example>
/// <code language="csharp">
/// var command = ToolCommandLine.Parse(["scaffold", "api", "Orders"]);
/// // command.Mode == ToolMode.Scaffold, command.Application == new ApplicationSpec(ApplicationKind.Api, "Orders")
/// </code>
/// </example>
public static class ToolCommandLine
{
    /// <summary>
    /// The usage line of the <c>bootstrap</c> command, shown when its arguments are malformed.
    /// </summary>
    public const string BootstrapUsage = "anarchy-toolkit bootstrap [<solution-name> [--output <empty-directory>] [--with-skills] [--layering folder|prefix]]";

    /// <summary>
    /// The <c>bootstrap</c> flag that also installs the toolkit's agent skills.
    /// </summary>
    public const string WithSkills = "--with-skills";

    /// <summary>
    /// The usage line of the <c>skills</c> command, shown when its arguments are malformed.
    /// </summary>
    public const string SkillsUsage = "anarchy-toolkit skills install [--output <solution-directory>]";

    /// <summary>
    /// The usage line of the <c>boilerplate</c> command, shown when its arguments are malformed.
    /// </summary>
    public const string BoilerplateUsage = "anarchy-toolkit boilerplate list|show <path>|diff [<path>...]";

    /// <summary>
    /// The <c>scaffold api</c> flag that generates an API without the toolkit's default Swagger UI theme.
    /// </summary>
    public const string NoSwaggerTheme = "--no-swagger-theme";

    /// <summary>
    /// The usage line of the <c>scaffold</c> command, shown when its arguments are malformed.
    /// </summary>
    public const string ScaffoldUsage = "anarchy-toolkit scaffold [api <Name> [--no-swagger-theme]|job <Name>|ui <Name>]";

    /// <summary>
    /// Parses the arguments, without the executable name. Never throws: malformed input yields a
    /// <see cref="ToolMode.Invalid"/> command carrying the error.
    /// </summary>
    /// <param name="args">The command-line arguments, without the executable name.</param>
    /// <returns>
    /// The parsed command: <see cref="ToolMode.Help"/> for no arguments or a help flag, <see cref="ToolMode.Bootstrap"/>,
    /// <see cref="ToolMode.Scaffold"/>, <see cref="ToolMode.Skills"/> or <see cref="ToolMode.Boilerplate"/> (with their
    /// optional arguments), or
    /// <see cref="ToolMode.Invalid"/>.
    /// </returns>
    public static ToolCommand Parse(IReadOnlyList<string> args) => args switch {
        [] or ["--help" or "-h" or "help", ..] => new ToolCommand(ToolMode.Help),
        ["bootstrap", ..] => ParseBootstrap(args.Skip(1).ToArray()),
        ["scaffold", ..] => ParseScaffold(args.Skip(1).ToArray()),
        ["skills", ..] => ParseSkills(args.Skip(1).ToArray()),
        ["boilerplate", ..] => ParseBoilerplate(args.Skip(1).ToArray()),
        [var command, ..] => Invalid($"Unknown command '{command}'.")
    };

    private static ToolCommand ParseBootstrap(IReadOnlyList<string> args)
    {
        if (args.Count == 0) return new ToolCommand(ToolMode.Bootstrap);
        if (args[0].StartsWith('-')) return Invalid("Usage: " + BootstrapUsage);

        string? output = null;
        var skills = false;
        LayeringMode? layering = null;
        for (var index = 1; index < args.Count; index++)
        {
            switch (args[index])
            {
                case "--output" or "-o" when output is null && index + 1 < args.Count && !string.IsNullOrWhiteSpace(args[index + 1]):
                    output = args[++index];
                    break;
                case WithSkills when !skills:
                    skills = true;
                    break;
                case "--layering" when layering is null && index + 1 < args.Count:
                    layering = args[++index].ToLowerInvariant() switch {
                        "folder" => LayeringMode.Folder,
                        "prefix" => LayeringMode.Prefix,
                        _ => null
                    };
                    if (layering is null) return Invalid("Usage: " + BootstrapUsage);
                    break;
                default:
                    return Invalid("Usage: " + BootstrapUsage);
            }
        }
        return new ToolCommand(ToolMode.Bootstrap, args[0], output) { AgentSkills = skills, Layering = layering };
    }

    private static ToolCommand ParseSkills(IReadOnlyList<string> args) => args switch {
        ["install"] => new ToolCommand(ToolMode.Skills),
        ["install", "--output" or "-o", var output] when !string.IsNullOrWhiteSpace(output) =>
            new ToolCommand(ToolMode.Skills, OutputDirectory: output),
        _ => Invalid("Usage: " + SkillsUsage)
    };

    private static ToolCommand ParseBoilerplate(IReadOnlyList<string> args) => args switch {
        ["list"] => new ToolCommand(ToolMode.Boilerplate) { BoilerplateAction = "list" },
        ["show", var path] when !path.StartsWith('-') => new ToolCommand(ToolMode.Boilerplate) { BoilerplateAction = "show", Paths = [path] },
        ["diff", ..] when args.Skip(1).All(path => !path.StartsWith('-')) =>
            new ToolCommand(ToolMode.Boilerplate) { BoilerplateAction = "diff", Paths = args.Skip(1).ToArray() },
        _ => Invalid("Usage: " + BoilerplateUsage)
    };

    private static ToolCommand ParseScaffold(IReadOnlyList<string> args) => args switch {
        [] => new ToolCommand(ToolMode.Scaffold),
        [var kind, var name] when TryParseKind(kind, out var parsed) && !name.StartsWith('-') =>
            new ToolCommand(ToolMode.Scaffold, Application: new ApplicationSpec(parsed, name)),
        [var kind, var name, NoSwaggerTheme] when TryParseKind(kind, out var parsed) && parsed == ApplicationKind.Api && !name.StartsWith('-') =>
            new ToolCommand(ToolMode.Scaffold, Application: new ApplicationSpec(parsed, name, SwaggerTheme: false)),
        _ => Invalid("Usage: " + ScaffoldUsage)
    };

    private static bool TryParseKind(string value, out ApplicationKind kind) =>
        Enum.TryParse(value, ignoreCase: true, out kind) && Enum.IsDefined(kind);

    private static ToolCommand Invalid(string error) => new(ToolMode.Invalid, Error: error);
}
