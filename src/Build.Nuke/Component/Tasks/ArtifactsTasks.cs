using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using System.Text.Json;

namespace Anarchy.Toolkit.Build.Nuke.Component.Tasks
{
    public static class ArtifactsTasks
    {
        public static void PrintArtifactsJson(IDefaultBuild build, string? output)
        {
            var artifactDtos = build.Artifacts
                .Select(x => new ArtifactDto(x.ProjectFile, x.ProjectName, GetDeploymentType(x), x.CodeDeploy.ArchiveName))
                .ToArray();
            var json = JsonSerializer.Serialize(artifactDtos);

            if (string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine(json);
            }
            else
            {
                File.WriteAllText(output, json);
                Console.WriteLine($"Wrote artifacts json output into {output} file");
            }
        }

        private record ArtifactDto(string? ProjectFile, string? ProjectName, string DeploymentType, string? ArchiveName);

        private static string GetDeploymentType(Artifact artifact)
        {
            if (artifact.CodeDeploy.IsEnabled)
                return "CodeDeploy";

            if (artifact.AwsLambda.IsEnabled)
                return "AwsLambda";

            return "None";
        }
    }
}
