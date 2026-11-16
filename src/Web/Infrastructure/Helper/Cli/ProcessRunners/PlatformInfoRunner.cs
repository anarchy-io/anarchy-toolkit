namespace Anarchy.Toolkit.Web.Infrastructure.Helper.Cli.ProcessRunners;

/// <summary>Host platform information helpers built on <see cref="ProcessRunner"/> (Unix-like hosts).</summary>
public static class PlatformInfoRunner
{
    /// <summary>Returns the host CPU architecture reported by <c>uname -m</c> (e.g. <c>arm64</c>, <c>x86_64</c>).</summary>
    /// <remarks>
    /// Exceptions thrown synchronously are swallowed and yield an empty string; failures surfacing asynchronously
    /// (e.g. a non-zero exit code) propagate through the returned task.
    /// </remarks>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The architecture name, or an empty string.</returns>
    public static Task<string> GetHostCpuArchitectureAsync(CancellationToken cancellationToken = default)
    {
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
    /// <returns><see langword="true"/> when <c>uname -m</c> reports <c>arm64</c>; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> IsArmHostAsync(CancellationToken cancellationToken = default)
    {
        var architecture = await GetHostCpuArchitectureAsync(cancellationToken);
        return architecture == "arm64";
    }
}
