using Anarchy.Toolkit.Web.Infrastructure.Helper.Cli.Exceptions;
using System.Diagnostics;

namespace Anarchy.Toolkit.Web.Infrastructure.Helper.Cli;

public static class ProcessRunner
{
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
