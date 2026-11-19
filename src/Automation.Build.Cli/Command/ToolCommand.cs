using Anarchy.Toolkit.Automation.Core.Entity;

namespace Anarchy.Toolkit.Automation.Build.Cli.Command;

/// <summary>
/// The parsed command line. Without a solution name (bootstrap) or application (scaffold), the matching wizard runs.
/// </summary>
/// <param name="Mode">The command to run.</param>
/// <param name="SolutionName">The solution name given to <c>bootstrap</c>.</param>
/// <param name="OutputDirectory">
/// The <c>--output</c> directory given to <c>bootstrap</c> or <c>skills install</c>; when <c>null</c>, the working
/// directory, or the solution found from it for <c>skills install</c>, is used.
/// </param>
/// <param name="Application">The application given to <c>scaffold</c>.</param>
/// <param name="Error">Why the command line is invalid, when <paramref name="Mode"/> is <see cref="ToolMode.Invalid"/>.</param>
public sealed record ToolCommand(
    ToolMode Mode,
    string? SolutionName = null,
    string? OutputDirectory = null,
    ApplicationSpec? Application = null,
    string? Error = null)
{
    /// <summary>
    /// Whether <c>bootstrap</c> installs the toolkit's agent skills (<c>--with-skills</c>).
    /// </summary>
    public bool AgentSkills { get; init; }

    /// <summary>
    /// The layering mode requested for non-interactive bootstrap, or <see langword="null"/> for the default.
    /// </summary>
    public LayeringMode? Layering { get; init; }

    /// <summary>
    /// The <c>boilerplate</c> subcommand: <c>list</c>, <c>show</c> or <c>diff</c>.
    /// </summary>
    public string? BoilerplateAction { get; init; }

    /// <summary>
    /// The packaged paths given to <c>boilerplate show</c> (exactly one) or <c>boilerplate diff</c> (any).
    /// </summary>
    public IReadOnlyList<string> Paths { get; init; } = [];

    /// <summary>
    /// Whether the command needs a wizard, and therefore interactive input.
    /// </summary>
    public bool IsInteractive => (Mode == ToolMode.Bootstrap && SolutionName is null) ||
                                 (Mode == ToolMode.Scaffold && Application is null);
}
