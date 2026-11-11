using Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;
using Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Entity.Build;
using Anarchy.Toolkit.Build.Fallout.Infrastructure.Service.Tool.Pipenv;
using Fallout.Common;
using Fallout.Common.CI.TeamCity;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Utilities;
using Logger = Anarchy.Toolkit.Build.Fallout.Infrastructure.Helper.Logger;

namespace Anarchy.Toolkit.Build.Fallout.Component.Tasks
{
    /// <summary>
    /// Tasks related to AWS CodeDeploy pipelines.
    /// </summary>
    public static class CodeDeployTasks
    {
        public const string DefaultCodeDeployToolkitPath = "codedeploy-toolkit";

        /// <summary>
        /// Creates zip files out of all the published artifacts for the AWS CodeDeploy deployment flow. The zip files
        /// are named according to the <see cref="CodeDeployArtifactSettings.ArchiveName"/> property and are stored in the
        /// <see cref="ICodeDeployBuild.CodeDeployArtifactsDirectory"/>.
        /// </summary>
        public static void PackCodeDeployArtifacts(this ICodeDeployBuild build)
        {
            foreach (var artifact in build.Artifacts.Where(a => a.CodeDeploy.IsEnabled))
            {
                var artifactDirectory = build.ArtifactsDirectory / artifact.ProjectName / Runtime.Linux;
                artifactDirectory.ZipTo(archiveFile: build.CodeDeployArtifactsDirectory / artifact.CodeDeploy.ArchiveName + ".zip", fileMode: FileMode.Create);
            }
        }

        /// <summary>
        /// Validates that there is at least one artifact for which CodeDeploy deployment is enabled (by having
        /// properties not null or empty).
        /// </summary>
        public static void ValidateCodeDeploySettings(this ICodeDeployBuild build)
        {
            if (!build.Artifacts.Any(a => a.CodeDeploy.IsEnabled))
                Assert.Fail("None of the projects are configured to be deployed to AWS CodeDeploy.");

            Logger.Trace("CodeDeploy settings are valid.");
        }

        public static void DeployArtifacts(this ICodeDeployBuild build)
        {
            PipenvTasks.PipenvSync(s => s.SetProcessWorkingDirectory(build.CodeDeployToolkitAbsolutePath));
            PipenvTasks.PipenvDeployArtifacts(s => s.SetProcessWorkingDirectory(build.CodeDeployToolkitAbsolutePath)
                                                    .SetManifestPath(build.ManifestAbsolutePath));
        }

        /// <summary>
        /// Publishes a collection of projects for a specific runtime. It stores the output in a subdirectory underneath
        /// <see cref="IDefaultBuild.ArtifactsDirectory"/>, namely <c>{project name}/{runtime}</c>, e.g. <c>MyService/win10-x64</c>.
        /// It uses <see cref="IVersionedBuild.BuildNumber"/> as a version, if it exists, or otherwise a default of <c>0.0.1</c>.
        /// </summary>
        public static void PublishProjects(this ICodeDeployBuild build, Runtime runtime) =>
            DotNetTasks.DotNetPublish(
                s => s.SetConfiguration(build.Configuration)
                    .SetRuntime(runtime)
                    .DisableSelfContained()
                    .SetTreatWarningsAsErrors(build.TreatWarningsAsErrors)
                    .SetAssemblyVersion(build.BuildNumber ?? "0.0.1")
                    .SetFileVersion(build.BuildNumber ?? "0.0.1")
                    .SetInformationalVersion(
                        (build.BuildNumber ?? "0.0.1") +
                        (build.GitRepository != null
                            ? "-" + build.GitRepository.Commit
                            : "")
                    )
                    .CombineWith(
                        c => build.Artifacts.Where(a => a.CodeDeploy.IsEnabled).Select(
                            a => c.SetProject(a.ProjectFile)
                                .SetOutput(build.ArtifactsDirectory / a.ProjectName / runtime)
                        )
                    )
            );

        /// <summary>
        /// Writes <c>build.meta.json</c> files for all CodeDeploy artifacts.
        /// </summary>
        public static void WriteBuildMetadata(this ICodeDeployBuild build)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            string? buildNumber = build is IVersionedBuild versionedBuild
                                     ? versionedBuild.BuildNumber
                                     : null;
            var metadata = new BuildMetadataModel {
                BuildNumber = buildNumber,
                GitSha1 = build.GitRepository?.Commit,
            };

            if (build.Host is TeamCity teamCity)
                metadata.BuiltBy = teamCity.ConfigurationProperties["teamcity.build.triggeredBy"];

            foreach (var artifact in build.Artifacts.Where(a => a.CodeDeploy.IsEnabled))
            {
                var projectFile = artifact.ProjectFile
                                  ?? throw new InvalidOperationException("CodeDeploy artifacts need a ProjectFile.");
                var path = projectFile.Parent / "Properties" / "build.meta.json";
                path.WriteJson(metadata);

                Logger.Info("Wrote build metadata to {0}", path);
            }
        }
    }
}
