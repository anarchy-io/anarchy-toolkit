using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Tool.Command;

/// <summary>
/// The parsed command line. Without a solution name (bootstrap) or application (scaffold), the matching wizard runs.
/// </summary>
/// <param name="Mode">The command to run.</param>
/// <param name="SolutionName">The solution name given to <c>bootstrap</c>.</param>
/// <param name="OutputDirectory">
/// The <c>--output</c> directory given to <c>bootstrap</c>; the working directory is used when <c>null</c>.
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
    /// Whether the command needs a wizard, and therefore interactive input.
    /// </summary>
    public bool IsInteractive => (Mode == ToolMode.Bootstrap && SolutionName is null) ||
                                 (Mode == ToolMode.Scaffold && Application is null);
}
