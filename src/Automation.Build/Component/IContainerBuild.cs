using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.Docker;
using Fallout.Common.Tools.DotNet;
using Serilog;
using System.Runtime.InteropServices;
using static Fallout.Common.Tools.Docker.DockerTasks;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// A container flow: publishes one runnable app (<see cref="IStandardBuild.AppName"/>), builds it into
/// an image with Docker and pushes it to a registry. Only <see cref="ContainerRegistry"/> must be provided;
/// everything else has a sensible default.
/// </summary>
/// <remarks>
/// The targets chain as <see cref="ContainerPublishAppArtifact"/> → <see cref="ContainerImageBuild"/> → <see cref="ContainerImagePush"/>,
/// after <see cref="IStandardBuild.Compile"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// partial class Build : AnarchyBuild, IContainerBuild
/// {
///     public string ContainerRegistry => "123456789012.dkr.ecr.eu-west-1.amazonaws.com";
/// }
/// </code>
/// Then run <c>make container_image_push APP_NAME=Shop.Api</c>.
/// </example>
public interface IContainerBuild : IStandardBuild
{
    /// <summary>
    /// The registry images are pushed to, for example <c>123456789012.dkr.ecr.eu-west-1.amazonaws.com</c>.
    /// </summary>
    [Parameter("Container registry (required by image build/push)")]
    string ContainerRegistry => TryGetValue(() => ContainerRegistry) ?? Preference<string>("container.registry") ?? "";

    /// <summary>
    /// The image name; by default <c>{solution}-{app}</c>, lower case (e.g. <c>shop-api</c>).
    /// </summary>
    [Parameter("Container image name (default: lower-case solution-app)")]
    string ContainerImageName => TryGetValue(() => ContainerImageName) ?? Preference<string>("container.imageName") ?? $"{Solution.Name?.ToLowerInvariant()}-{AppName?.ToLowerInvariant()}";

    /// <summary>
    /// The image tag; by default the short (7 character) SHA of the current Git commit, or <c>null</c> outside a Git repository.
    /// </summary>
    [Parameter("Container image tag (default: short Git commit)")]
    string? ContainerImageTag => TryGetValue(() => ContainerImageTag) ?? (GitRepository?.Commit is { Length: > 7 } commit ? commit[..7] : GitRepository?.Commit);

    /// <summary>
    /// The full image reference, <c>{registry}/{name}:{tag}</c>, that is built and pushed.
    /// </summary>
    string ContainerImageReference => $"{ContainerRegistry}/{ContainerImageName}:{ContainerImageTag}";

    /// <summary>
    /// The Dockerfile building the image, <c>docker/Dockerfile</c> by default; it copies the app from
    /// <see cref="ContainerAppArtifactDirectory"/>.
    /// </summary>
    [Parameter("Container Dockerfile (default: docker/Dockerfile)")]
    AbsolutePath Dockerfile => TryGetValue(() => Dockerfile) ?? PreferencePath("container.dockerfile") ?? RootDirectory / "docker" / "Dockerfile";

    /// <summary>
    /// Folder the application is published to and that the Dockerfile copies from:
    /// <c>artifacts/Container/{app}</c>, lower case.
    /// </summary>
    AbsolutePath ContainerAppArtifactDirectory => ArtifactsDirectory / "Container" / (AppName?.ToLowerInvariant() ?? "");

    /// <summary>
    /// The runtime the app is published for; by default the Linux flavour of the host's architecture.
    /// </summary>
    [Parameter("Container publish runtime (default: Linux host architecture)")]
    string ContainerRuntimeIdentifier => TryGetValue(() => ContainerRuntimeIdentifier) ?? Preference<string>("container.runtimeIdentifier") ??
        (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64");

    /// <summary>
    /// Runs the unit tests (<see cref="IStandardBuild.TestUnit"/>), then publishes <see cref="IStandardBuild.AppName"/> framework-dependent for <see cref="ContainerRuntimeIdentifier"/>
    /// into <see cref="ContainerAppArtifactDirectory"/>. Requires an app name and an image tag (a Git repository).
    /// </summary>
    Target ContainerPublishAppArtifact => d => d
        .Description("Publishes a runnable application (framework-dependent) to be included in a container image, after the unit tests")
        .DependsOn(Compile, TestUnit)
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

    /// <summary>
    /// Builds <see cref="ContainerImageReference"/> from <see cref="Dockerfile"/>, with the repository root as context
    /// and the <c>ROOT_DIR</c> and <c>APP_NAME</c> (lower case) build arguments.
    /// </summary>
    Target ContainerImageBuild => d => d
        .Description("Builds a container image for the published application")
        .Requires(() => !string.IsNullOrWhiteSpace(ContainerRegistry))
        .DependsOn(ContainerPublishAppArtifact)
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

    /// <summary>
    /// Pushes <see cref="ContainerImageReference"/> to the registry. The Docker client must already be logged in to
    /// <see cref="ContainerRegistry"/>.
    /// </summary>
    Target ContainerImagePush => d => d
        .Description("Pushes the container image to the configured registry")
        .DependsOn(ContainerImageBuild)
        .Executes(() => {
            Log.Information("Pushing image {ImageRef}", ContainerImageReference);

            DockerPush(s => s
                .SetName(ContainerImageReference)
                .EnableProcessOutputLogging());
        });
}
