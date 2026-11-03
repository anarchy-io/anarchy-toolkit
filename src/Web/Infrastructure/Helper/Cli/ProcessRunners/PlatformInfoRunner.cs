namespace Anarchy.Toolkit.Web.Infrastructure.Helper.Cli.ProcessRunners;

public static class PlatformInfoRunner
{
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

    public static async Task<bool> IsArmHostAsync(CancellationToken cancellationToken = default)
    {
        var architecture = await GetHostCpuArchitectureAsync(cancellationToken);
        return architecture == "arm64";
    }
}
