using Fallout.Common;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Opts integration and end-to-end tests into starting the local Compose environment and waiting for the configured
/// <see cref="IDockerBuild.DockerWaitFor"/> containers. Unit tests do not use this component.
/// </summary>
public interface ITestEnvironmentBuild : IDockerBuild
{
    /// <summary>
    /// Starts the local environment and runs Flyway migrations only if the build also implements <see cref="IFlywayBuild"/>.
    /// Leaves the environment running and does not remove volumes.
    /// </summary>
    Target PrepareTestEnvironment => d => d
        .Description("Prepare the local environment for integration and end-to-end tests")
        .DependsOn(DockerStart)
        .TryDependsOn<IFlywayBuild>(x => x.FlywayMigrate);
}
