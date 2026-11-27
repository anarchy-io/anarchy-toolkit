using Anarchy.Toolkit.Runtime.Web.Helpers.Cli.Exceptions;
using System.Diagnostics;

namespace Anarchy.Toolkit.Runtime.Web.Helpers.Cli;

/// <summary>
/// Runs external command-line processes and captures their output.
/// </summary>
public static class ProcessRunner
{
    /// <summary>
    /// Starts <paramref name="fileName"/> with the given arguments (no shell, no window), waits for it to exit and
    /// returns its trimmed standard output.
    /// </summary>
    /// <param name="fileName">The executable to run (resolved through <c>PATH</c>).</param>
    /// <param name="arguments">The arguments, passed individually without shell quoting.</param>
    /// <param name="cancellationToken">Token to cancel waiting for the process and reading its output.</param>
    /// <returns>The trimmed standard output.</returns>
    /// <exception cref="ProcessStartException">The process could not be started.</exception>
    /// <exception cref="ProcessExitCodeException">The process exited with a non-zero exit code; includes the trimmed standard error.</exception>
    public static async Task<string> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
                            ?? throw new ProcessStartException(fileName);

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = (await outputTask).Trim();
        var error = (await errorTask).Trim();

        if (process.ExitCode != 0)
        {
            throw new ProcessExitCodeException(fileName, process.ExitCode, error);
        }

        return output;
    }
}
