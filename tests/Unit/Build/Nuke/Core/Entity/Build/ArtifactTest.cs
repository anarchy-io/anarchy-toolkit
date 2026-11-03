using Anarchy.Toolkit.Build.Nuke.Core.Entity.Build;
using AwesomeAssertions;
using Nuke.Common.IO;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Core.Entity.Build;

public class ArtifactTest
{
    private static readonly AbsolutePath ProjectFile = AbsolutePath.Create(Path.GetTempPath()) / "src" / "Sync" / "Sync.csproj";

    [Fact]
    public void ProjectName_GivenProjectFile_IsItsDirectoryName()
    {
        // Act
        var artifact = new Artifact { ProjectFile = ProjectFile };

        // Assert
        artifact.ProjectName.Should().Be("Sync");
    }

    [Fact]
    public void ProjectName_GivenNoProjectFile_IsNull()
    {
        // Act & Assert
        new Artifact().ProjectName.Should().BeNull();
    }

    [Fact]
    public void DeploymentSettings_GivenNothingConfigured_AreDisabled()
    {
        // Act
        var artifact = new Artifact { ProjectFile = ProjectFile };

        // Assert
        artifact.CodeDeploy.IsEnabled.Should().BeFalse();
        artifact.AwsLambda.IsEnabled.Should().BeFalse();
        artifact.AwsLambda.IsLocalDeploymentEnabled.Should().BeFalse();
    }

    [Fact]
    public void DeploymentSettings_GivenArchiveNames_AreEnabled()
    {
        // Act
        var artifact = new Artifact {
            ProjectFile = ProjectFile,
            CodeDeploy = { ArchiveName = "anarchy-io-api" },
            AwsLambda = {
                ArchiveName = ProjectFile.Parent / "Anarchy.Io.Api.zip",
                FunctionName = "Anarchy.Io.Api",
                TestEventFile = ProjectFile.Parent / "event.json",
            },
        };

        // Assert
        artifact.CodeDeploy.IsEnabled.Should().BeTrue();
        artifact.AwsLambda.IsEnabled.Should().BeTrue();
        artifact.AwsLambda.IsLocalDeploymentEnabled.Should().BeTrue();
    }

    [Fact]
    public void ConfigurationValue_GivenString_KeepsValue()
    {
        // Act
        ConfigurationValue plain = "value";
        ConfigurationValue secret = new Secret("encrypted");

        // Assert
        plain.Value.Should().Be("value");
        secret.Should().BeOfType<Secret>().Which.Value.Should().Be("encrypted");
    }
}
