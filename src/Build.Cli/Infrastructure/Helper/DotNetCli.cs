using System.Diagnostics;

namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

/// <summary>Runs <c>dotnet</c> commands, keeping their output out of the console unless they fail.</summary>
public static class DotNetCli
{
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
