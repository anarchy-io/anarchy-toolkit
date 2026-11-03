using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Helper;
using Anarchy.Toolkit.Hosting.Workers.Service.Health;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestAssemblies;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Service.Health;

public class ApplicationInformationTests
{
    private ApplicationInformation CreateSut() =>
        ApplicationInformation.WithCustomMetadata(
            buildMetadata: new StubBuildMetadata(),
            applicationStartTime: DateTimeOffset.Now
        );

    private ApplicationInformation CreateSutWithStartTimeAndCustomClock(
        DateTimeOffset applicationStartTime,
        DateTimeOffset systemTime
    )
    {
        var sut = ApplicationInformation.WithCustomMetadata(
            buildMetadata: new StubBuildMetadata(),
            applicationStartTime: applicationStartTime
        );
        sut.Clock = () => systemTime;

        return sut;
    }

    [Fact]
    public void ApplicationUptimeShouldReturnDifferenceBetweenNowAndStartTime()
    {
        var applicationStartTime = new DateTimeOffset(2017, 4, 10, 8, 27, 23, TimeSpan.FromHours(2));
        var systemTime = new DateTimeOffset(2017, 4, 10, 9, 30, 45, TimeSpan.FromHours(2));
        var expectedUptime = TimeSpan.FromSeconds(3802);

        var sut = CreateSutWithStartTimeAndCustomClock(
            applicationStartTime: applicationStartTime,
            systemTime: systemTime
        );

        sut.Uptime.Should().Be(expectedUptime);
    }

    [Fact]
    public void ClockNotOverriddenShouldReportSystemTimeCorrectly()
    {
        var sut = CreateSut();

        sut.SystemTime.Should().BeCloseTo(DateTimeOffset.Now, precision: TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void CreateWithNullBuildMetadataShouldThrowException()
    {
        Action create = () => ApplicationInformation.WithCustomMetadata(
                            buildMetadata: null!,
                            applicationStartTime: DateTimeOffset.Now
                        );

        create.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateWithNullAssemblyShouldThrowException()
    {
        Action create = () => ApplicationInformation.WithMetadataFromAssembly(
                            assembly: null!,
                            applicationStartTime: DateTimeOffset.Now
                        );

        create.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithCustomMetadataBuildMetadataPropertiesShouldReflectBuildMetadataConstructorArgument()
    {
        var buildMetadata = new StubBuildMetadata();

        var sut = CreateSut();

        sut.Should().BeEquivalentTo(
            new {
                ApplicationName = buildMetadata.ApplicationName,
                ApplicationVersion = buildMetadata.ApplicationVersion,
                BuildCommitHash = buildMetadata.BuildCommitHash,
                BuildMachineName = buildMetadata.BuildMachineName,
                BuildTimestamp = buildMetadata.BuildTimestamp,
                BuildUsername = buildMetadata.BuildUsername,
                BuildNumber = buildMetadata.BuildNumber,
                RunbookUri = buildMetadata.RunbookUri,
                OperatingSystem = OperatingSystemInfo.Current,
            }
        );
    }

    [Fact]
    public void WithAssemblyMetadataBuildMetadataPropertiesShouldReflectAssemblyMetadata()
    {
        var sut = ApplicationInformation.WithMetadataFromAssembly(
            assembly: WithMetadataHelper.Assembly,
            applicationStartTime: DateTimeOffset.Now
        );

        // see TestAssemblies/build.meta.json for the origin of these values
        sut.ApplicationName.Should().Be(WithMetadataHelper.AssemblyName);
        sut.ApplicationVersion.Should().Be(WithMetadataHelper.Version);
        sut.BuildMachineName.Should().Be("BUILD_MACHINE");
        sut.BuildUsername.Should().Be("username");
        sut.BuildTimestamp.Should().Be(new DateTimeOffset(2017, 4, 10, 16, 0, 23, TimeSpan.FromHours(2)));
        sut.BuildCommitHash.Should().Be("4d1c60112414122951e6d2a4d3c75146452ecdd4");
        sut.BuildNumber.Should().Be("1234.0");
        sut.RunbookUri.Should().BeNull();
        sut.OperatingSystem.Should().Be(OperatingSystemInfo.Current);
    }

    private class StubBuildMetadata : IBuildMetadata
    {
        public string ApplicationName => "Application";
        public string ApplicationVersion => "1.0.0";
        public string TargetFramework => ".NETCoreApp,Version=v6.0";
        public string BuildNumber => "1.0.0-dev-9c0ff94421b3171045b7dca1ba180dbbe0cb6435";
        public string BuildMachineName => "Machine";
        public string BuildUsername => "Username";
        public DateTimeOffset? BuildTimestamp => new DateTimeOffset(2017, 4, 10, 10, 56, 49, TimeSpan.FromHours(2));
        public string BuildCommitHash => "9c0ff94421b3171045b7dca1ba180dbbe0cb6435";
        public string RunbookUri => "https://url.com/runbook";
    }
}
