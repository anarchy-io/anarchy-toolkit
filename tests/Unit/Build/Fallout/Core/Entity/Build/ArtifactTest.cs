using Anarchy.Toolkit.Build.Cli.Core.Entity;
using Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;
using AwesomeAssertions;
using Fallout.Common.IO;

namespace Anarchy.Toolkit.Tests.Unit.Build.Fallout.Core.Entity.Build;

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
