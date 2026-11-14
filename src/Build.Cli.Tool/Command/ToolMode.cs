namespace Anarchy.Toolkit.Build.Cli.Tool.Command;

/// <summary>
/// The command selected on the command line.
/// </summary>
public enum ToolMode
{
    /// <summary>
    /// No command, or <c>--help</c>, <c>-h</c> or <c>help</c>: show the usage.
    /// </summary>
    Help,

    /// <summary>
    /// <c>bootstrap</c>: create a new solution.
    /// </summary>
    Bootstrap,

    /// <summary>
    /// <c>scaffold</c>: add an application to an existing solution.
    /// </summary>
    Scaffold,

    /// <summary>
    /// An unknown command or malformed arguments; <see cref="ToolCommand.Error"/> says why.
    /// </summary>
    Invalid
}
