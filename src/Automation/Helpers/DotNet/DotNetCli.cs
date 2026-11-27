using System.Diagnostics;

namespace Anarchy.Toolkit.Automation.Helpers.DotNet;

/// <summary>
/// Runs <c>dotnet</c> commands, keeping their output out of the console unless they fail.
/// </summary>
public static class DotNetCli
{
    /// <summary>
    /// Runs <c>dotnet</c> with <paramref name="arguments"/> in <paramref name="workingDirectory"/> and waits for it to exit.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown when the command fails; the message includes its output.</exception>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="arguments">The <c>dotnet</c> arguments, e.g. <c>sln</c>, <c>Shop.sln</c>, <c>add</c>, a project path.</param>
    public static void Run(string workingDirectory, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start dotnet.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"dotnet {string.Join(" ", arguments)} failed with exit code {process.ExitCode}.{Environment.NewLine}{output.Result}{error.Result}".TrimEnd());
    }
}
