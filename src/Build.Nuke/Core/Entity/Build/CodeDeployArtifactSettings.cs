
namespace Anarchy.Toolkit.Build.Nuke.Core.Entity.Build
{
    partial class Artifact
    {
        public CodeDeployArtifactSettings CodeDeploy { get; } = new CodeDeployArtifactSettings();
    }

    /// <summary>
    /// Artifact settings specific to AWS CodeDeploy deployments.
    /// </summary>
    public sealed class CodeDeployArtifactSettings
    {
        /// <summary>
        /// Name of the zip file pushed to AWS CodeDeploy. This needs to match the name of the
        /// <c>CodeDeployApplication</c> resource in CloudFormation.
        /// </summary>
        public string? ArchiveName { get; set; }

        /// <summary>
        /// Returns whether AWS CodeDeploy deployment is enabled for the artifact.
        /// </summary>
        public bool IsEnabled => !string.IsNullOrEmpty(ArchiveName);
    }
}
