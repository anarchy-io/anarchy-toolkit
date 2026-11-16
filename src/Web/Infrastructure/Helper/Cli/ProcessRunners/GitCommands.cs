namespace Anarchy.Toolkit.Web.Infrastructure.Helper.Cli.ProcessRunners;

/// <summary>Git command helpers built on <see cref="ProcessRunner"/>.</summary>
public static class GitCommands
{
    /// <summary>Returns the abbreviated commit hash of <c>HEAD</c> (<c>git rev-parse --short HEAD</c>).</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The short commit hash.</returns>
    /// <exception cref="Anarchy.Toolkit.Web.Infrastructure.Helper.Cli.Exceptions.ProcessStartException">Git could not be started.</exception>
    /// <exception cref="Anarchy.Toolkit.Web.Infrastructure.Helper.Cli.Exceptions.ProcessExitCodeException">Git failed (e.g. not a repository).</exception>
    public static Task<string> GetShortHeadAsync(CancellationToken cancellationToken = default)
    {
        return ProcessRunner.RunAsync(
            "git",
            ["rev-parse", "--short", "HEAD"],
            cancellationToken);
    }
}
