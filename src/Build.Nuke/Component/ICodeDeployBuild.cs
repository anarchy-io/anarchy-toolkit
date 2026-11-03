using Anarchy.Toolkit.Build.Nuke.Component.Tasks;
using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using Anarchy.Toolkit.Build.Nuke.Core.ValueSet.Build;
using Nuke.Common;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.IO;

namespace Anarchy.Toolkit.Build.Nuke.Component
{
    public interface ICodeDeployBuild : IVersionedBuild
    {
        /// <summary>
        /// Directory where applications packaged for CodeDeploy are stored. By default refers to 'packaged', inside <see cref="IDefaultBuild.ArtifactsDirectory"/>.
        /// </summary>
        AbsolutePath CodeDeployArtifactsDirectory => ArtifactsDirectory / "packaged";

        [Parameter]
        string CodeDeployToolkitPath => TryGetValue(() => CodeDeployToolkitPath) ?? CodeDeployTasks.DefaultCodeDeployToolkitPath;

        [Parameter]
        string? ManifestPath => TryGetValue(() => ManifestPath);

        /// <summary>
        /// Directory where codedeploy-toolkit is stored. Refers to <see cref="CodeDeployToolkitPath"/> inside <see cref="INukeBuild.RootDirectory"/>.
        /// </summary>
        AbsolutePath CodeDeployToolkitAbsolutePath => RootDirectory / CodeDeployToolkitPath;

        /// <summary>
        /// Path to deployment artifacts manifest. Refers to <see cref="ManifestPath"/> inside <see cref="INukeBuild.RootDirectory"/>.
        /// </summary>
        AbsolutePath ManifestAbsolutePath => RootDirectory / (ManifestPath ?? "");

        /// <summary>
        /// Writes <c>build.meta.json</c> files for all CodeDeploy artifacts in <see cref="IDefaultBuild.Artifacts"/>. This will include
        /// information like the name of the build machine, the build timestamp, the Git commit at the time of building,
        /// and (if available) who triggered the build.
        /// </summary>
        Target WriteBuildMetadata =>
            _ => _
                .After(Clean)
                .Unlisted()
                .Executes(() => CodeDeployTasks.WriteBuildMetadata(this));

        /// <summary>
        /// Build a release for the AWS CodeDeploy deployment flow. This involves executing <see cref="Build"/>,
        /// <see cref="IDefaultBuild.Test"/>, and <see cref="PackCodeDeploy"/>, and their dependencies. It triggers
        /// <see cref="IReportFailureOnSlack.ReportFailure"/>, meaning a failure will be reported on Slack, if that has been configured
        /// correctly.
        /// </summary>
        Target BuildReleaseCodeDeploy =>
            _ => _
                .Description("Build release (CodeDeploy)")
                .DependsOn(Build, Test, PackCodeDeploy);

        /// <summary>
        /// Publishes all artifacts for the Linux runtime, which is used in the AWS CodeDeploy deployment
        /// flow. Depended on by <see cref="PackCodeDeploy"/>.
        /// </summary>
        Target PublishLinux =>
            _ => _
                .DependsOn(Clean, WriteBuildMetadata)
                .Unlisted()
                .Executes(
                     () => this.PublishProjects(Runtime.Linux)
                 );

        /// <summary>
        /// Creates zip files out of all the published artifacts for the AWS CodeDeploy deployment flow. The zip files
        /// are named according to the <see cref="CodeDeployArtifactSettings.ArchiveName"/> property and are stored in the
        /// <see cref="CodeDeployArtifactsDirectory"/>.
        /// </summary>
        Target PackCodeDeploy =>
            _ => _
                .DependsOn(PublishLinux, ValidateCodeDeploySettings)
                .Unlisted()
                .Triggers(AnnounceCodeDeployArtifactsToTeamCity)
                .Executes(this.PackCodeDeployArtifacts);

        /// <summary>
        /// Announces the directory containing the zipped artifacts to TeamCity. These will then be picked up by
        /// TeamCity as build artifacts. This is only done when actually running on TeamCity.
        /// </summary>
        Target AnnounceCodeDeployArtifactsToTeamCity =>
            _ => _
                .Unlisted()
                .OnlyWhenDynamic(() => IsServerBuild)
                .Executes(
                     () => TeamCity.Instance.PublishArtifacts(CodeDeployArtifactsDirectory)
                 );

        /// <summary>
        /// <para>
        /// This merely exposes a top-level target, which is used by CodeDeploy pipelines to validate the application
        /// has been properly deployed and functions as it should.
        /// </para>
        /// <para>
        /// When <see cref="IHasAcceptanceTests"/> is implemented, it will let <see cref="IHasAcceptanceTests.AcceptanceTest"/>
        /// be triggered by this target.
        /// </para>
        /// </summary>
        Target ValidateDeploymentAcceptance =>
            _ => _.Description("Validate deployment to Acceptance");

        /// <summary>
        /// Validates that there is at least one artifact for which CodeDeploy deployment is enabled (by having
        /// properties not null or empty). Fails the build otherwise. This is depended on by
        /// <see cref="PackCodeDeploy"/>.
        /// </summary>
        Target ValidateCodeDeploySettings =>
            _ => _
                .Unlisted()
                .Executes(() => CodeDeployTasks.ValidateCodeDeploySettings(this));

        /// <summary>
        /// Deploys artifacts to an environment via codedeploy-toolkit in <see cref="CodeDeployToolkitAbsolutePath"/>
        /// </summary>
        Target DeployArtifacts =>
            _ => _
                 .Description("Deploy artifacts (CodeDeploy)")
                 .Executes(() => CodeDeployTasks.DeployArtifacts(this));
    }
}
