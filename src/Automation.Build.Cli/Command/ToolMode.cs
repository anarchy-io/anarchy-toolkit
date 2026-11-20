namespace Anarchy.Toolkit.Automation.Build.Cli.Command;

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
    /// <c>skills install</c>: install the toolkit's agent skills into a solution.
    /// </summary>
    Skills,

    /// <summary>
    /// <c>boilerplate list|show|diff</c>: read the packaged boilerplate files or compare them with the solution's.
    /// </summary>
    Boilerplate,

    /// <summary><c>config show|set|unset|configure</c>: inspect or explicitly edit solution preferences.</summary>
    Config,

    /// <summary>
    /// An unknown command or malformed arguments; <see cref="ToolCommand.Error"/> says why.
    /// </summary>
    Invalid
}
