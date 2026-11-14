namespace Anarchy.Toolkit.Build.Cli.Core.Service.Container;

/// <summary>
/// Runs the <c>docker</c> and Compose executables; captured or streamed to the console.
/// </summary>
public interface IContainerCli
{
    /// <summary>
    /// Runs a command and captures its output; a command that cannot be started reports a non-zero exit code.
    /// </summary>
    (int ExitCode, string Output, string Error) Capture(string executable, IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string>? environment = null);

    /// <summary>
    /// Runs a command with its output streamed to the console, returning its exit code.
    /// </summary>
    int Stream(string executable, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment = null);
}
