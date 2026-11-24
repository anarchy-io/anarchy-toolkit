using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Anarchy.Toolkit.Automation.Core.Service.Container;
using Fallout.Common;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Runs Flyway in one-shot Docker containers. With <see cref="ITestEnvironmentBuild"/>, migration runs after
/// Compose startup and readiness checks and before integration or end-to-end tests.
/// </summary>
public interface IFlywayBuild : IFalloutBuild
{
    /// <summary>The Flyway CLI image, including a tag or digest.</summary>
    [Parameter("Flyway CLI image (default: redgate/flyway:11.8.2)")]
    string FlywayImage => TryGetValue(() => FlywayImage) ?? "redgate/flyway:11.8.2";

    /// <summary>Flyway configuration files, relative to the repository root or absolute, in loading order.</summary>
    [Parameter("Flyway configuration files to mount, in loading order")]
    string[] FlywayConfigFiles => TryGetValue(() => FlywayConfigFiles) ?? [];

    /// <summary>Migration directories to mount, relative to the repository root or absolute.</summary>
    [Parameter("Migration directories to mount as Flyway filesystem locations")]
    string[] FlywayLocations => TryGetValue(() => FlywayLocations) ?? [];

    /// <summary>The Docker network the container joins, or <see langword="null"/> to use Docker's default.</summary>
    [Parameter("Docker network for the Flyway container")]
    string? FlywayNetwork => TryGetValue(() => FlywayNetwork);

    /// <summary>The Docker CLI used to launch the Flyway container.</summary>
    IContainerCli FlywayContainerCli => new ProcessContainerCli(RootDirectory);

    /// <summary>Applies pending migrations. Prepares Compose first only when the build opts into test preparation.</summary>
    Target FlywayMigrate => d => d
        .Description("Apply Flyway migrations in a one-shot container")
        .TryDependsOn<ITestEnvironmentBuild>(x => x.DockerStart)
        .Executes(() => RunFlyway("migrate"));

    /// <summary>Reports migration status without starting the local Compose environment.</summary>
    Target FlywayInfo => d => d
        .Description("Show Flyway migration status in a one-shot container")
        .Executes(() => RunFlyway("info"));

    private void RunFlyway(string command) => FlywayTasks.Run(FlywayContainerCli, RootDirectory, FlywayImage,
        FlywayNetwork, FlywayConfigFiles, FlywayLocations, command);
}
