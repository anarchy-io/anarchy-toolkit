using Fallout.Common;

namespace Anarchy.Toolkit.Build.Fallout.Component
{
    /// <summary>
    /// For solutions shipping both AWS Lambda and CodeDeploy artifacts: one target releasing both.
    /// </summary>
    public interface IFullReleaseBuild : IAwsLambdaBuild, ICodeDeployBuild
    {
        Target BuildReleaseFull => d => d
            .Description("Builds the full solution: Lambdas and CodeDeploy")
            .DependsOn(PublishLambda, BuildReleaseCodeDeploy);
    }
}
