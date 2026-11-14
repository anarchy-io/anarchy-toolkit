using Anarchy.Toolkit.Build.Cli.Core.Service.Container;
using Fallout.Common;
using Fallout.Common.IO;

namespace Anarchy.Toolkit.Build.Fallout.Component;

/// <summary>
/// Local container management on the repository's Docker Compose file (by default <c>docker/docker-compose.yml</c>):
/// start, stop, restart and destroy the environment, follow its logs, and wait for containers to be running or
/// healthy.
/// </summary>
public interface IDockerBuild : IFalloutBuild
{
    [Parameter("Docker Compose file (default: docker/docker-compose.yml)")]
    AbsolutePath DockerComposeFile => TryGetValue(() => DockerComposeFile) ?? RootDirectory / "docker" / "docker-compose.yml";

    [Parameter("Docker Compose project name (default: the repository directory's name)")]
    string DockerProjectName => TryGetValue(() => DockerProjectName) ?? LocalContainerTasks.ProjectNameOf(RootDirectory);

    [Parameter("Container to show the logs of, restart or wait for")]
    string? DockerContainer => TryGetValue(() => DockerContainer);

    [Parameter("Extra arguments for docker compose up or logs")]
    string? DockerArgs => TryGetValue(() => DockerArgs);

    [Parameter("Containers DockerStart waits for")]
    string[] DockerWaitFor => TryGetValue(() => DockerWaitFor) ?? [];

    [Parameter("Wait for the containers to be healthy rather than running")]
    bool DockerWaitForHealthy => TryGetValue<bool?>(() => DockerWaitForHealthy) ?? false;

    [Parameter("Seconds to wait for a container to become healthy (default: 60; running, for DockerWaitRunning)")]
    int DockerTimeout => TryGetValue<int?>(() => DockerTimeout) ?? LocalContainerTasks.DefaultTimeout;

    [Parameter("Seconds to wait for a container to start before checking its health (default: 180)")]
    int DockerStartTimeout => TryGetValue<int?>(() => DockerStartTimeout) ?? LocalContainerTasks.DefaultStartTimeout;

    LocalContainerTasks Containers => new(RootDirectory, DockerComposeFile, DockerProjectName);

    Target DockerInfo => d => d.Description("Show the Docker engine, Compose command and running containers")
        .Executes(() => Containers.Info());

    Target DockerStopAll => d => d.Description("Stop every Docker container running on the host")
        .Executes(() => Containers.StopAll());

    Target DockerLogs => d => d.Description("Print the environment's container logs")
        .Executes(() => Containers.Logs(DockerContainer, LocalContainerTasks.SplitArguments(DockerArgs)));

    Target DockerStart => d => d.Description("Start the local environment, then wait for DockerWaitFor containers")
        .Executes(() => Containers.Start(LocalContainerTasks.SplitArguments(DockerArgs), DockerWaitFor, DockerWaitForHealthy,
            DockerTimeout, DockerStartTimeout));

    Target DockerStop => d => d.Description("Stop the local environment")
        .Executes(() => Containers.Stop());

    Target DockerRestart => d => d.Description("Restart the local environment, or DockerContainer")
        .Executes(() => Containers.Restart(DockerContainer));

    Target DockerDestroy => d => d.Description("Remove the local environment's containers, networks and volumes")
        .Executes(() => Containers.Destroy());

    Target DockerWaitRunning => d => d.Description("Wait for DockerContainer to be running")
        .Requires(() => DockerContainer)
        .Executes(() => Containers.WaitForRunning(DockerContainer!, DockerTimeout));

    Target DockerWaitHealthy => d => d.Description("Wait for DockerContainer to be running, then healthy")
        .Requires(() => DockerContainer)
        .Executes(() => Containers.WaitForHealthy(DockerContainer!, DockerTimeout, DockerStartTimeout));
}
