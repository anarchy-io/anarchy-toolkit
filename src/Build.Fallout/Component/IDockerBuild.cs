using Anarchy.Toolkit.Build.Cli.Core.Service.Container;
using Fallout.Common;
using Fallout.Common.IO;

namespace Anarchy.Toolkit.Build.Fallout.Component;

/// <summary>
/// Local container management on the repository's Docker Compose file (by default <c>docker/docker-compose.yml</c>):
/// start, stop, restart and destroy the environment, follow its logs, and wait for containers to be running or
/// healthy.
/// </summary>
/// <remarks>
/// It derives from <see cref="IFalloutBuild"/> directly, so it needs no solution. The work is done by
/// <see cref="Anarchy.Toolkit.Build.Cli.Core.Service.Container.LocalContainerTasks"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// partial class Build : FalloutBuild, IStandardBuild, IDockerBuild
/// {
///     Func&lt;string, bool&gt;? IDockerBuild.DockerReadinessCheck => container => container != "localstack" || LocalStackIsReady();
/// }
/// </code>
/// Then run <c>fallout docker-start --docker-wait-for postgres --docker-wait-for-healthy</c>.
/// </example>
public interface IDockerBuild : IFalloutBuild
{
    /// <summary>
    /// The Docker Compose file (<c>--docker-compose-file</c>); defaults to <c>docker/docker-compose.yml</c>.
    /// </summary>
    [Parameter("Docker Compose file (default: docker/docker-compose.yml)")]
    AbsolutePath DockerComposeFile => TryGetValue(() => DockerComposeFile) ?? RootDirectory / "docker" / "docker-compose.yml";

    /// <summary>
    /// The Docker Compose project name (<c>--docker-project-name</c>); defaults to the repository directory's name.
    /// </summary>
    [Parameter("Docker Compose project name (default: the repository directory's name)")]
    string DockerProjectName => TryGetValue(() => DockerProjectName) ?? LocalContainerTasks.ProjectNameOf(RootDirectory);

    /// <summary>
    /// The container <see cref="DockerLogs"/>, <see cref="DockerRestart"/>, <see cref="DockerWaitRunning"/> and
    /// <see cref="DockerWaitHealthy"/> act on (<c>--docker-container</c>); all containers when not set, where allowed.
    /// </summary>
    [Parameter("Container to show the logs of, restart or wait for")]
    string? DockerContainer => TryGetValue(() => DockerContainer);

    /// <summary>
    /// Extra arguments for <c>docker compose up</c> (<see cref="DockerStart"/>) or <c>docker compose logs</c>
    /// (<see cref="DockerLogs"/>) (<c>--docker-args</c>).
    /// </summary>
    [Parameter("Extra arguments for docker compose up or logs")]
    string? DockerArgs => TryGetValue(() => DockerArgs);

    /// <summary>
    /// The containers <see cref="DockerStart"/> waits for once the environment is up (<c>--docker-wait-for</c>); none by
    /// default.
    /// </summary>
    [Parameter("Containers DockerStart waits for")]
    string[] DockerWaitFor => TryGetValue(() => DockerWaitFor) ?? [];

    /// <summary>
    /// Whether <see cref="DockerStart"/> waits for <see cref="DockerWaitFor"/> to be healthy rather than just running
    /// (<c>--docker-wait-for-healthy</c>). Defaults to <see langword="false"/>.
    /// </summary>
    [Parameter("Wait for the containers to be healthy rather than running")]
    bool DockerWaitForHealthy => TryGetValue<bool?>(() => DockerWaitForHealthy) ?? false;

    /// <summary>
    /// Seconds to wait for a container to become healthy, or running for <see cref="DockerWaitRunning"/>
    /// (<c>--docker-timeout</c>). Defaults to 60.
    /// </summary>
    [Parameter("Seconds to wait for a container to become healthy (default: 60; running, for DockerWaitRunning)")]
    int DockerTimeout => TryGetValue<int?>(() => DockerTimeout) ?? LocalContainerTasks.DefaultTimeout;

    /// <summary>
    /// Seconds to wait for a container to start before checking its health (<c>--docker-start-timeout</c>). Defaults
    /// to 180.
    /// </summary>
    [Parameter("Seconds to wait for a container to start before checking its health (default: 180)")]
    int DockerStartTimeout => TryGetValue<int?>(() => DockerStartTimeout) ?? LocalContainerTasks.DefaultStartTimeout;

    /// <summary>
    /// An extra readiness check <see cref="DockerWaitRunning"/> runs once <see cref="DockerContainer"/> is running: called
    /// once a second with the container's name until it returns <see langword="true"/>, for up to
    /// <see cref="DockerTimeout"/> seconds. Override it to check containers that
    /// have no health check of their own (e.g. probe a port or an HTTP endpoint); none by default.
    /// </summary>
    Func<string, bool>? DockerReadinessCheck => null;

    /// <summary>
    /// The container operations on <see cref="DockerComposeFile"/> and <see cref="DockerProjectName"/> that the targets
    /// delegate to. A new instance is created on every access.
    /// </summary>
    LocalContainerTasks Containers => new(RootDirectory, DockerComposeFile, DockerProjectName);

    /// <summary>
    /// Shows the Docker engine, the Compose command in use and the running containers.
    /// </summary>
    Target DockerInfo => d => d.Description("Show the Docker engine, Compose command and running containers")
        .Executes(() => Containers.Info());

    /// <summary>
    /// Stops every Docker container running on the host, not only the environment's.
    /// </summary>
    Target DockerStopAll => d => d.Description("Stop every Docker container running on the host")
        .Executes(() => Containers.StopAll());

    /// <summary>
    /// Prints the logs of the environment, or of <see cref="DockerContainer"/>, passing <see cref="DockerArgs"/> on.
    /// </summary>
    Target DockerLogs => d => d.Description("Print the environment's container logs")
        .Executes(() => Containers.Logs(DockerContainer, LocalContainerTasks.SplitArguments(DockerArgs)));

    /// <summary>
    /// Starts the environment (passing <see cref="DockerArgs"/> on), then waits for the <see cref="DockerWaitFor"/>
    /// containers.
    /// </summary>
    Target DockerStart => d => d.Description("Start the local environment, then wait for DockerWaitFor containers")
        .Executes(() => Containers.Start(LocalContainerTasks.SplitArguments(DockerArgs), DockerWaitFor, DockerWaitForHealthy,
            DockerTimeout, DockerStartTimeout));

    /// <summary>
    /// Stops the environment's containers.
    /// </summary>
    Target DockerStop => d => d.Description("Stop the local environment")
        .Executes(() => Containers.Stop());

    /// <summary>
    /// Restarts the environment, or only <see cref="DockerContainer"/> when set.
    /// </summary>
    Target DockerRestart => d => d.Description("Restart the local environment, or DockerContainer")
        .Executes(() => Containers.Restart(DockerContainer));

    /// <summary>
    /// Removes the environment's containers, networks and volumes.
    /// </summary>
    Target DockerDestroy => d => d.Description("Remove the local environment's containers, networks and volumes")
        .Executes(() => Containers.Destroy());

    /// <summary>
    /// Waits up to <see cref="DockerTimeout"/> seconds for <see cref="DockerContainer"/> (required) to be running, then
    /// for <see cref="DockerReadinessCheck"/>, if any.
    /// </summary>
    Target DockerWaitRunning => d => d.Description("Wait for DockerContainer to be running")
        .Requires(() => DockerContainer)
        .Executes(() => Containers.WaitForRunning(DockerContainer!, DockerTimeout, DockerReadinessCheck));

    /// <summary>
    /// Waits for <see cref="DockerContainer"/> (required) to be running, within <see cref="DockerStartTimeout"/> seconds,
    /// then healthy, within <see cref="DockerTimeout"/> seconds.
    /// </summary>
    Target DockerWaitHealthy => d => d.Description("Wait for DockerContainer to be running, then healthy")
        .Requires(() => DockerContainer)
        .Executes(() => Containers.WaitForHealthy(DockerContainer!, DockerTimeout, DockerStartTimeout));
}
