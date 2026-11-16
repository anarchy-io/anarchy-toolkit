namespace Anarchy.Toolkit.Build.Cli.Core.Service.Container;

/// <summary>
/// Runs the <c>docker</c> and Compose executables; captured or streamed to the console.
/// </summary>
/// <remarks>Abstracts process execution so that <see cref="LocalContainerTasks"/> can be tested without Docker.</remarks>
public interface IContainerCli
{
    /// <summary>
    /// Runs a command and captures its output; a command that cannot be started reports a non-zero exit code.
    /// </summary>
    /// <param name="executable">The executable, e.g. <c>docker</c>.</param>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="environment">Extra environment variables, or <see langword="null"/>.</param>
    /// <returns>The exit code and the captured standard output and error.</returns>
    (int ExitCode, string Output, string Error) Capture(string executable, IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string>? environment = null);

    /// <summary>
    /// Runs a command with its output streamed to the console, returning its exit code.
    /// </summary>
    /// <param name="executable">The executable, e.g. <c>docker</c>.</param>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="environment">Extra environment variables, or <see langword="null"/>.</param>
    /// <returns>The exit code.</returns>
    int Stream(string executable, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment = null);
}
