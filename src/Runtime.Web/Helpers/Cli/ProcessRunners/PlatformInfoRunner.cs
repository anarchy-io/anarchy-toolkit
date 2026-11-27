using System.Runtime.InteropServices;

namespace Anarchy.Toolkit.Runtime.Web.Helpers.Cli.ProcessRunners;

/// <summary>Host platform information helpers.</summary>
public static class PlatformInfoRunner
{
    /// <summary>Returns the host CPU architecture (e.g. <c>arm64</c>, <c>x86_64</c>), using <c>uname -m</c> on Unix.</summary>
    /// <remarks>
    /// On Unix, exceptions thrown synchronously while starting the command are swallowed and yield an empty string;
    /// failures surfacing asynchronously (e.g. a non-zero exit code) propagate through the returned task. On Windows,
    /// a cancelled token throws before reading the operating system architecture.
    /// </remarks>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The architecture name, or an empty string.</returns>
    public static Task<string> GetHostCpuArchitectureAsync(CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsWindows())
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(RuntimeInformation.OSArchitecture switch {
                Architecture.X64 => "x86_64",
                Architecture.Arm64 => "arm64",
                var architecture => architecture.ToString().ToLowerInvariant(),
            });
        }

        try
        {
            return ProcessRunner.RunAsync(
                "uname",
                ["-m"],
                cancellationToken);
        }
        catch
        {
            return Task.FromResult(string.Empty);
        }
    }

    /// <summary>Determines whether the host CPU architecture is <c>arm64</c>.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the host architecture is <c>arm64</c>; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> IsArmHostAsync(CancellationToken cancellationToken = default)
    {
        var architecture = await GetHostCpuArchitectureAsync(cancellationToken);
        return architecture == "arm64";
    }
}
