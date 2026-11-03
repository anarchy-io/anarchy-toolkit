namespace Anarchy.Toolkit.Web.Infrastructure.Helper.Cli.ProcessRunners;

public static class GitCommands
{
    public static Task<string> GetShortHeadAsync(CancellationToken cancellationToken = default)
    {
        return ProcessRunner.RunAsync(
            "git",
            ["rev-parse", "--short", "HEAD"],
            cancellationToken);
    }
}
