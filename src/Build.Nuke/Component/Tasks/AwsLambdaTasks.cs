using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.Aws;
using Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.AwsSam;
using Anarchy.Toolkit.Build.Nuke.Infrastructure.Service.Tool.DotNetLambda;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using System.Text.Json;

namespace Anarchy.Toolkit.Build.Nuke.Component.Tasks
{
    /// <summary>
    /// Tasks for packing and uploading AWS Lambdas
    /// </summary>
    internal static class AwsLambdaTasks
    {
        /// <summary>
        /// Creates zip files out of all projects with AWS Lambda deployment flow enabled. The zip files
        /// are named and stored according to the <see cref="AwsLambdaArtifactSettings.ArchiveName"/> property.
        /// </summary>
        public static void PublishLambdas(this IVersionedBuild build) =>
            DotNetLambdaTasks.DotNetLambdaPackage(
                s => s.SetConfiguration(build.Configuration)
                      .CombineWith(
                           c => build.Artifacts.Where(a => a.AwsLambda.IsEnabled)
                                     .Select(
                                          a => c.SetOutputPackage(a.AwsLambda.ArchiveName)
                                                .SetProjectLocation(Path.GetDirectoryName(a.ProjectFile))
                                                .SetFunctionArchitecture(a.AwsLambda.FunctionArchitecture)
                                     )
                      )
            );

        public static void PublishLambdasCI(this IAwsLambdaBuild build) =>
            DotNetLambdaTasks.DotNetLambdaPackageCI(
                s => s.SetConfiguration(build.Configuration)
                      .CombineWith(
                           c => build.Artifacts
                                     .Select(
                                          a => c.SetProjectLocation(Path.GetDirectoryName(a.ProjectFile))
                                                .SetS3Bucket(build.SamArtifactsBucket)
                                                .SetOutputTemplate(build.OutputCloudformationTemplateFile)
                                                .SetConfiguration(build.Configuration)
                                                .SetDisableInteractive(true)
                                     )
                      )
            );

        /// <summary>
        /// Packs and uploads published AWS Lambda artficats <see cref="PublishLambdas(IVersionedBuild)"/> to AWS.
        /// Template file name is set according to <see cref="IAwsLambdaBuild.CloudformationTemplateFile"/>,
        /// output template file is set according to <see cref="IAwsLambdaBuild.OutputCloudformationTemplateFile"/>
        /// and s3 bucket is set according to <see cref="IAwsLambdaBuild.SamArtifactsBucket"/>
        /// </summary>
        public static void AwsCliPackageLambdas(this IAwsLambdaBuild build) =>
            AwsTasks.AwsLambdaPackage(
                s => s.SetTemplateFile(build.CloudformationTemplateFile)
                      .SetOutputTemplateFile(build.OutputCloudformationTemplateFile)
                      .SetS3Bucket(build.SamArtifactsBucket)
            );

        /// <summary>
        /// Write development cloudformation parameters to <see cref="IAwsLambdaBuild.DevelopmentCloudformationParametersFile"/>
        /// by prompting user to fill in parameters from <see cref="IAwsLambdaBuild.DevelopmentCloudformationParametersFile"/>
        /// </summary>
        public static void ConfigureDevelopmentCloudformationParameters(this IAwsLambdaBuild build)
        {
            var cloudformationParameters = GetCloudformationParameters(build.CloudformationParametersTemplateFile);

            foreach (var cloudformationParameter in cloudformationParameters)
            {
                var userInput = ConsoleUtility.PromptForInput(question: cloudformationParameter.ParameterKey, defaultValue: cloudformationParameter.ParameterValue);
                cloudformationParameter.ParameterValue = userInput;
            }

            var developmentCloudformationParametersJson = JsonSerializer.Serialize(cloudformationParameters);
            File.WriteAllText(build.DevelopmentCloudformationParametersFile, developmentCloudformationParametersJson);
        }

        /// <summary>
        /// Invokes AWS Lambdas using AWS SAM cli. 
        /// Template is set according to <see cref="IAwsLambdaBuild.CloudformationTemplateFile"/>, 
        /// parameter overrides is set according to <see cref="IAwsLambdaBuild.DevelopmentCloudformationParametersFile"/>,
        /// event file is set according to <see cref="AwsLambdaArtifactSettings.TestEventFile"/>,
        /// function name is set according to <see cref="AwsLambdaArtifactSettings.FunctionName"/>
        /// </summary>
        public static void DeployLambdasLocal(this IAwsLambdaBuild build) =>
            AwsSamTasks.AwsSamLocalInvoke(
                s => s.SetTemplate(build.CloudformationTemplateFile)
                      .SetParameterOverrides(GetCloudformationParameters(build.DevelopmentCloudformationParametersFile).ToDictionary(cp => cp.ParameterKey, cp => (object)cp.ParameterValue))
                      .CombineWith(
                           c => build.Artifacts.Where(a => a.AwsLambda.IsLocalDeploymentEnabled)
                                     .Select(
                                          a => c.SetEvent(a.AwsLambda.TestEventFile)
                                                .SetFunctionName(a.AwsLambda.FunctionName)
                                     )
                      )
            );

        private static CloudformationParameter[] GetCloudformationParameters(AbsolutePath filePath)
        {
            var json = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<CloudformationParameter[]>(json) ?? [];
        }

        private class CloudformationParameter
        {
            public string ParameterKey { get; set; } = "";
            public string ParameterValue { get; set; } = "";
        }
    }
}
