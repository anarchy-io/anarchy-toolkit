using Anarchy.Toolkit.Hosting.Workers.Service.Health;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestAssemblies;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Service.Health;

public class BuildMetadataFromEmbeddedResourceTests
{
    [Fact]
    public void ReadsNameAndVersionFromAssembly()
    {
        var sut = new BuildMetadataFromEmbeddedResource(WithMetadataHelper.Assembly);

        sut.ApplicationName.Should().Be(WithMetadataHelper.AssemblyName);
        sut.ApplicationVersion.Should().Be(WithMetadataHelper.Version);
    }

    [Fact]
    public void ReadsFieldsFromEmbeddedJson()
    {
        var sut = new BuildMetadataFromEmbeddedResource(WithMetadataHelper.Assembly);

        // see TestAssemblies/build.meta.json for the origin of these values
        sut.BuildMachineName.Should().Be("BUILD_MACHINE");
        sut.BuildUsername.Should().Be("username");
        sut.BuildTimestamp.Should().Be(new DateTimeOffset(2017, 4, 10, 16, 0, 23, TimeSpan.FromHours(2)));
        sut.BuildCommitHash.Should().Be("4d1c60112414122951e6d2a4d3c75146452ecdd4");
        sut.BuildNumber.Should().Be("1234.0");
    }

    [Fact]
    public void WhenResourceDoesNotExistShouldNotThrow()
    {
        BuildMetadataFromEmbeddedResource? sut = null;
        Action action =
            () => sut = new BuildMetadataFromEmbeddedResource(WithoutMetadataHelper.Assembly);

        action.Should().NotThrow();

        const string metadataNotFoundMessage = "build.meta.json not found";

        sut!.BuildMachineName.Should().Be(metadataNotFoundMessage);
        sut.BuildUsername.Should().Be(metadataNotFoundMessage);
        sut.BuildTimestamp.Should().Be(null);
        sut.BuildCommitHash.Should().Be(metadataNotFoundMessage);
        sut.BuildNumber.Should().Be(metadataNotFoundMessage);
    }

    [Fact]
    public void CreateWithNullAssemblyShouldThrowException()
    {
        Action create = () => new BuildMetadataFromEmbeddedResource(assembly: null!);

        create.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReadsRunbookUriFromAssemblyAttribute()
    {
        var sut = new BuildMetadataFromEmbeddedResource(WithAssemblyAttributesHelper.Assembly);

        sut.RunbookUri.Should().Be("https://some.com/runbook");
    }
}
