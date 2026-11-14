using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.Docker;
using Fallout.Common.Tools.DotNet;
using Serilog;
using System.Runtime.InteropServices;
using static Fallout.Common.Tools.Docker.DockerTasks;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Anarchy.Toolkit.Build.Fallout.Component;

/// <summary>
/// A container flow: publishes one runnable app (<see cref="IStandardBuild.AppName"/>), builds it into
/// an image with Docker and pushes it to a registry (ECR). Only <see cref="ContainerRegistry"/> must be provided;
/// everything else has a sensible default.
/// </summary>
public interface IContainerBuild : IStandardBuild
{
    /// <summary>
    /// The registry images are pushed to, for example <c>123456789012.dkr.ecr.eu-west-1.amazonaws.com</c>.
    /// </summary>
    string ContainerRegistry { get; }

    /// <summary>
    /// The image name; by default <c>{solution}-{app}</c>, lower case (e.g. <c>shop-api</c>).
    /// </summary>
    string ContainerImageName => $"{Solution.Name?.ToLowerInvariant()}-{AppName?.ToLowerInvariant()}";

    /// <summary>
    /// The image tag; by default the short (7 character) SHA of the current Git commit, or <c>null</c> outside a Git repository.
    /// </summary>
    string? ContainerImageTag => GitRepository?.Commit is { Length: > 7 } commit ? commit[..7] : GitRepository?.Commit;

    /// <summary>
    /// The full image reference, <c>{registry}/{name}:{tag}</c>, that is built and pushed.
    /// </summary>
    string ContainerImageReference => $"{ContainerRegistry}/{ContainerImageName}:{ContainerImageTag}";

    /// <summary>
    /// The Dockerfile building the image, <c>docker/ecs/Dockerfile</c> by default; it copies the app from
    /// <see cref="ContainerAppArtifactDirectory"/>.
    /// </summary>
    AbsolutePath Dockerfile => RootDirectory / "docker" / "ecs" / "Dockerfile";

    /// <summary>
    /// Folder the application is published to and that the Dockerfile copies from:
    /// <c>artifacts/AwsEcr/{app}</c>, lower case.
    /// </summary>
    AbsolutePath ContainerAppArtifactDirectory => ArtifactsDirectory / "AwsEcr" / (AppName?.ToLowerInvariant() ?? "");

    /// <summary>
    /// The runtime the app is published for; by default the Linux flavour of the host's architecture.
    /// </summary>
    string ContainerRuntimeIdentifier =>
        RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";

    Target EcrPublishAppArtifact => d => d
        .Description("Publishes a runnable application (framework-dependent) to be included in a container image")
        .DependsOn(Compile)
        .Requires(() => !string.IsNullOrWhiteSpace(ContainerImageTag))
        .Requires(() => !string.IsNullOrWhiteSpace(AppName))
        .Executes(() => {
            var appProject = ResolveRunnableProjectOrFail(AppName!);

            Log.Information("Publishing {AppName} ({Platform}) to {Output}",
                appProject.Name,
                ContainerRuntimeIdentifier,
                ContainerAppArtifactDirectory);

            DotNetPublish(s => s
                .SetProject(appProject)
                .SetConfiguration(Configuration)
                .SetVerbosity(ToolVerbosity)
                .SetRuntime(ContainerRuntimeIdentifier)
                .DisableSelfContained()
                .SetOutput(ContainerAppArtifactDirectory));
        });

    Target EcrImageBuild => d => d
        .Description("Builds a container image for the published application")
        .DependsOn(EcrPublishAppArtifact)
        .Executes(() => {
            Log.Information("Building image {ImageRef}", ContainerImageReference);

            DockerBuild(s => s
                .SetPath(RootDirectory)
                .SetBuildArg($"ROOT_DIR={RootDirectory}")
                .SetBuildArg($"APP_NAME={AppName?.ToLowerInvariant()}")
                .SetFile(Dockerfile)
                .SetTag(ContainerImageReference)
                .EnableProcessOutputLogging());
        });

    Target EcrImagePush => d => d
        .Description("Pushes the container image to the configured registry")
        .DependsOn(EcrImageBuild)
        .Executes(() => {
            Log.Information("Pushing image {ImageRef}", ContainerImageReference);

            DockerPush(s => s
                .SetName(ContainerImageReference)
                .EnableProcessOutputLogging());
        });
}
