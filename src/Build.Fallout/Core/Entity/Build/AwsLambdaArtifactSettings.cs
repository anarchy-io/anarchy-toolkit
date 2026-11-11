using Fallout.Common.IO;

namespace Anarchy.Toolkit.Build.Fallout.Core.Entity.Build
{
    partial class Artifact
    {
        public AwsLambdaArtifactSettings AwsLambda { get; } = new AwsLambdaArtifactSettings();
    }

    public sealed class AwsLambdaArtifactSettings
    {
        /// <summary>
        /// Absolute path to the zip file pushed to AWS Lambda. This needs to match the path of the
        /// <c>CodeUri</c> function resource property in CloudFormation.
        ///
        /// From 3.3.0 this can be left null and the  <c>CodeUri</c> function resource property in CloudFormation
        /// can just point to the Lambda project folder
        /// </summary>
        public AbsolutePath? ArchiveName { get; set; }

        /// <summary>
        /// The name of the function as specified in CloudFormation.
        /// </summary>
        public string? FunctionName { get; set; }

        /// <summary>
        /// The path to the JSON file contains event data to be passed to the Lambda function when it's invoked.
        /// </summary>
        public AbsolutePath? TestEventFile { get; set; }

        /// <summary>
        /// Returns whether AWS Lambda deployment is enabled for the artifact.
        /// </summary>
        public bool IsEnabled => !string.IsNullOrEmpty(ArchiveName);

        /// <summary>
        /// Returns whether AWS Lambda local deployment is enabled for the artifact.
        /// </summary>
        public bool IsLocalDeploymentEnabled => !string.IsNullOrEmpty(FunctionName) && !string.IsNullOrEmpty(TestEventFile);

        /// <summary>
        /// The architecture of the Lambda function. Valid values: x86_64 or arm64. Default is x86_64
        /// </summary>
        public string? FunctionArchitecture { get; set; }
    }
}
