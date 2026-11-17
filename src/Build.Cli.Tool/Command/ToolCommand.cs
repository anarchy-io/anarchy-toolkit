using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Tool.Command;

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
    /// Whether the command needs a wizard, and therefore interactive input.
    /// </summary>
    public bool IsInteractive => (Mode == ToolMode.Bootstrap && SolutionName is null) ||
                                 (Mode == ToolMode.Scaffold && Application is null);
}
