namespace Anarchy.Toolkit.Build.Cli.Core.Service.Container;

/// <summary>
/// An <see cref="IContainerCli"/> running real processes in a working directory.
/// </summary>
public sealed class ProcessContainerCli(string root) : IContainerCli
{
    /// <inheritdoc/>
    /// <remarks>An executable that cannot be found yields exit code <c>-1</c> with the error message.</remarks>
    public (int ExitCode, string Output, string Error) Capture(string executable, IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        try
        {
            return LocalProcessTasks.Run(root, executable, arguments, environment);
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            return (-1, "", exception.Message);
        }
    }

    /// <inheritdoc/>
    public int Stream(string executable, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment = null) =>
        LocalProcessTasks.Run(root, executable, arguments, environment, capture: false).ExitCode;
}
