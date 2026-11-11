using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using Fallout.Common;
using Fallout.Common.IO;

namespace Anarchy.Toolkit.Build.Fallout.Component
{
    public interface IAwsLambdaBuild : IVersionedBuild
    {
        /// <summary>
        /// Bucket where AWS Lambdas cloudformation is stored. By default refers to "SAM_ARTIFACTS_BUCKET" environment variable
        /// </summary>
        [Parameter("Bucket where AWS Lambdas cloudformation is stored (typically set by environment variable).", Name = "SAM_ARTIFACTS_BUCKET")]
        public string? SamArtifactsBucket => TryGetValue(() => SamArtifactsBucket);

        /// <summary>
        /// The path and file name where your AWS SAM template is located. By default refers to "cloudformation.yaml" in the root directory
        /// </summary>
        public AbsolutePath CloudformationTemplateFile => RootDirectory / "cloudformation.yaml";

        /// <summary>
        /// The path to the file where the command writes the packaged template. By default refers to "packaged.yaml" in the root directory
        /// </summary>
        public AbsolutePath OutputCloudformationTemplateFile => RootDirectory / "packaged.yaml";

        /// <summary>
        /// The path to the directory containing cloudformation parameters. By default refers to "cloudformation" folder in
        /// the root directory
        /// </summary>
        public AbsolutePath CloudformationDirectory => RootDirectory / "cloudformation";

        /// <summary>
        /// The path to the JSON file with cloudformation parameters template. By default refers to "cloudformation/parameters.json.template"
        /// in the root directory
        /// </summary>
        public AbsolutePath CloudformationParametersTemplateFile => CloudformationDirectory / "parameters.json.template";

        /// <summary>
        /// The path to the JSON file with development cloudformation parameters which are written when executing <see cref="IDefaultBuild.SetupLocal"/> targer.
        /// By default refers to "cloudformation/development.parameters.json" in the root directory
        /// </summary>
        public AbsolutePath DevelopmentCloudformationParametersFile => CloudformationDirectory / "development.parameters.json";

        /// <summary>
        /// Indicates whether to use V1 AWS Lambda deployment or V2.
        /// V1 requires specifying archive name manually and consists of "dotnet lambda package" and "aws cloudformation package"
        /// V2 only requires CodeUri in CloudFormation pointing to the project folder and combines 2 commands from above into 1 via "dotnet lambda package-ci"
        /// </summary>
        public bool UseV2Deployment => false;

        /// <summary>
        /// Configures local cloudformation parameters for development.
        /// </summary>
        Target ConfigureLocalCloudformationParameters =>
            _ => _.Executes(this.ConfigureDevelopmentCloudformationParameters)
                  .Unlisted()
                  .TriggeredBy(SetupLocal);

        /// <summary>
        /// Deploys lambdas locally using aws sam cli.
        /// </summary>
        Target DeployLocal =>
            _ => _.DependsOn(PublishLambda)
                  .Executes(this.DeployLambdasLocal);

        /// <summary>
        /// Execute <see cref="IDefaultBuild.Clean"/>, <see cref="IDefaultBuild.Build"/>, <see cref="IDefaultBuild.Test"/>, <see cref="PublishLambda"/>
        /// and <see cref="DeployLocal"/> for local development.
        /// </summary>
        Target Run =>
            _ => _.DependsOn(Clean, Build, Test, PublishLambda, DeployLocal);

        /// <summary>
        /// Package AWS Lambdas using dotnet lambda cli.
        /// </summary>
        Target PublishLambda =>
            _ => _.DependsOn(Clean)
                  .OnlyWhenDynamic(() => !UseV2Deployment)
                  .Executes(this.PublishLambdas);

        /// <summary>
        /// Upload published artifacts to AWS.
        /// </summary>
        Target AwsCliPackage =>
            _ => _.DependsOn(PublishLambda)
                  .OnlyWhenDynamic(() => !UseV2Deployment)
                  .After(Test)
                  .Executes(this.AwsCliPackageLambdas);

        /// <summary>
        /// Package and upload AWS Lambdas using dotnet lambda cli.
        /// </summary>
        Target PublishLambdaCI =>
            _ => _.DependsOn(Clean)
                  .OnlyWhenDynamic(() => UseV2Deployment)
                  .After(Test)
                  .Executes(this.PublishLambdasCI);


        /// <summary>
        /// Combines <see cref="PublishLambda"/> and <see cref="AwsCliPackage"/> and only done when running on TeamCity.
        /// </summary>
        Target DeployServer =>
            _ => _.DependsOn(PublishLambda, AwsCliPackage, PublishLambdaCI)
                  .Unlisted()
                  .OnlyWhenDynamic(() => IsServerBuild);

        /// <summary>
        /// Build a release for the AWS Lambda deployment flow. This involves executing <see cref="Build"/>,
        /// <see cref="IDefaultBuild.Test"/>, <see cref="PublishLambda"/> and <see cref="AwsCliPackage"/>. It triggers
        /// <see cref="IReportFailureOnSlack.ReportFailure"/>, meaning a failure will be reported on Slack, if that has been configured 
        /// correctly.
        /// </summary>
        Target BuildRelease =>
            _ => _.Description("Build relase (AWS Lambda)")
                  .DependsOn(Clean, Build, Test, DeployServer);
    }
}
