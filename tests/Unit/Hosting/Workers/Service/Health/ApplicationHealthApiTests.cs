using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Entity.Service;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using Anarchy.Toolkit.Hosting.Workers.Helper;
using Anarchy.Toolkit.Hosting.Workers.Service.Health;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestAssemblies;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestHelpers;
using Anarchy.Toolkit.Web.Presentation.Entity.Output.Health;
using AwesomeAssertions;
using System.Net;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Service.Health;

public class ApplicationHealthApiTests
{
    private ApplicationHealthApi CreateSut()
    {
        return ApplicationHealthApi.WithCustomMetadata(_reports, new StubBuildMetadata(), _applicationStartTime);
    }

    [Fact]
    public async Task ReturnsHealthAsHealthCheckReportJson()
    {
        _reports.Add(new FixedReport(new HealthCheckEntity(
            "Health test 1", HealthCheckResult.Failed.ToDescription(),
            new DateTimeOffset(2017, 4, 7, 16, 42, 23, 123, TimeSpan.Zero), TimeSpan.FromMilliseconds(1250))));
        _reports.Add(new FixedReport(new HealthCheckEntity(
            "Health test 2", HealthCheckResult.Passed.ToDescription(),
            new DateTimeOffset(2017, 4, 7, 16, 43, 35, 334, TimeSpan.Zero), TimeSpan.FromMilliseconds(1750))));

        var result = await CreateSut().GetHealthHttpResponse(TestContext.Current.CancellationToken);

        var json = JsonNode.Parse(result.Content)!;
        json["Passed"]!.GetValue<bool>().Should().BeFalse();
        json["Duration"].Should().NotBeNull();

        var tests = json["Tests"]!.AsArray();
        tests.Should().HaveCount(2);
        tests[0]!["Name"]!.GetValue<string>().Should().Be("Health test 1");
        tests[0]!["Result"]!.GetValue<string>().Should().Be("failed");
        tests[0]!["TestedAt"]!.GetValue<string>().Should().StartWith("2017-04-07T16:42:23.123");
        tests[0]!["Duration"]!.GetValue<string>().Should().Be("00:00:01.2500000");
        tests[1]!["Result"]!.GetValue<string>().Should().Be("passed");
    }

    [Fact]
    public async Task HealthJsonHasTheSameShapeAsTheWebHealthController()
    {
        _reports.Add(new StubServiceHealthReport("passing"));
        _reports.Add(new FixedReport(new HealthCheckEntity(
            "failing", HealthCheckResult.Failed, TimeSpan.FromMilliseconds(5), new InvalidOperationException("boom"))));
        var sut = CreateSut();

        var report = await sut.GetHealthReportAsync(TestContext.Current.CancellationToken);
        var webJson = HealthCheckReportOutput.FromDto(report).ToJson();

        JsonNode.DeepEquals(JsonNode.Parse(report.ToJson()), JsonNode.Parse(webJson)).Should().BeTrue();
    }

    [Fact]
    public async Task AllReportsPassReturnsStatusCodeOk()
    {
        _reports.Add(new StubServiceHealthReport("Health test"));

        var result = await CreateSut().GetHealthHttpResponse(TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NoReportsReturnsStatusCodeOk()
    {
        var result = await CreateSut().GetHealthHttpResponse(TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Should().Contain("\"Tests\":[]");
    }

    [Fact]
    public async Task SingleReportFailsReturnsStatusCodeInternalServerError()
    {
        _reports.Add(new StubServiceHealthReport("Health test"));
        _reports.Add(new StubServiceHealthReport("Health test", HealthCheckResult.Failed));

        var result = await CreateSut().GetHealthHttpResponse(TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void WithCustomMetadataReturnsInformationAsProperlyFormattedJson()
    {
        _applicationStartTime = new DateTimeOffset(2017, 4, 10, 14, 53, 23, TimeSpan.FromHours(2));
        var sut = CreateSut();
        sut.ApplicationInformation.Clock = () => new DateTimeOffset(2017, 4, 10, 14, 59, 12, TimeSpan.FromHours(2));

        var result = sut.GetApplicationInformationAsJson();

        AssertJsonMatches(
            result,
            new {
                artifact_id = "Application",
                build_number = "1.0.0-dev-9c0ff94421b3171045b7dca1ba180dbbe0cb6435",
                build_machine = "Machine",
                built_by = "username",
                built_when = "2017-04-10T14:51:23+02:00",
                current_time = "2017-04-10T14:59:12+02:00",
                git_sha1 = "9c0ff94421b3171045b7dca1ba180dbbe0cb6435",
                machine_name = Environment.MachineName,
                runbook_uri = "http://some.com/runbook",
                up_duration = 349,
                up_since = "2017-04-10T14:53:23+02:00",
                version = "1.0.0",
                operating_system = OperatingSystemInfo.Current,
                runtime_version = Environment.Version.ToString(),
                target_framework = ".NETCoreApp,Version=v6.0",
            }
        );
    }

    [Fact]
    public void WithMetadataFromAssemblyReturnsInformationAsProperlyFormattedJson()
    {
        _applicationStartTime = new DateTimeOffset(2017, 4, 10, 14, 53, 23, TimeSpan.FromHours(2));
        var sut = ApplicationHealthApi.WithMetadataFromAssembly(
            _reports,
            WithMetadataHelper.Assembly,
            _applicationStartTime
        );
        sut.ApplicationInformation.Clock = () => new DateTimeOffset(2017, 4, 10, 14, 59, 12, TimeSpan.FromHours(2));

        var result = sut.GetApplicationInformationAsJson();

        // see TestAssemblies/build.meta.json for the origin of these values
        AssertJsonMatches(
            result,
            new {
                artifact_id = WithMetadataHelper.AssemblyName,
                build_number = "1234.0",
                build_machine = "BUILD_MACHINE",
                built_by = "username",
                built_when = "2017-04-10T16:00:23+02:00",
                current_time = "2017-04-10T14:59:12+02:00",
                git_sha1 = "4d1c60112414122951e6d2a4d3c75146452ecdd4",
                machine_name = Environment.MachineName,
                runbook_uri = (string?)null,
                up_duration = 349,
                up_since = "2017-04-10T14:53:23+02:00",
                version = WithMetadataHelper.Version,
                operating_system = OperatingSystemInfo.Current,
                runtime_version = Environment.Version.ToString(),
                target_framework = GetTargetFramework(),
            }
        );
    }

    [Fact]
    public void WithCustomMetadataNullReportCollectionShouldThrowException()
    {
        Action create = () => ApplicationHealthApi.WithCustomMetadata(
                            reports: null!,
                            buildMetadata: new StubBuildMetadata(),
                            applicationStartTime: _applicationStartTime
                        );

        create.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithCustomMetadataNullBuildMetadataShouldThrowException()
    {
        Action create = () => ApplicationHealthApi.WithCustomMetadata(
                            reports: _reports,
                            buildMetadata: null!,
                            applicationStartTime: _applicationStartTime
                        );

        create.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithMetadataFromAssemblyNullReportCollectionShouldThrowException()
    {
        Action create = () => ApplicationHealthApi.WithMetadataFromAssembly(
                            reports: null!,
                            assembly: WithMetadataHelper.Assembly,
                            applicationStartTime: _applicationStartTime
                        );

        create.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithMetadataFromAssemblyNullAssemblyShouldThrowException()
    {
        Action create = () => ApplicationHealthApi.WithMetadataFromAssembly(
                            reports: _reports,
                            assembly: null!,
                            applicationStartTime: _applicationStartTime
                        );

        create.Should().Throw<ArgumentNullException>();
    }

    // The legacy version compared Newtonsoft JObjects via AwesomeAssertions.Json; System.Text.Json's structural
    // comparison (property order insensitive, dates kept as strings) is used instead.
    private static void AssertJsonMatches(string actual, object expected)
    {
        var actualJson = JsonNode.Parse(actual);
        var expectedJson = JsonSerializer.SerializeToNode(expected);

        JsonNode.DeepEquals(actualJson, expectedJson)
                .Should().BeTrue($"expected JSON {expectedJson?.ToJsonString()} but found {actualJson?.ToJsonString()}");
    }

    private static string GetTargetFramework()
    {
        var attribute = typeof(ApplicationHealthApiTests).Assembly.GetCustomAttribute<TargetFrameworkAttribute>();
        return attribute!.FrameworkName;
    }

    private readonly List<IServiceHealthReport> _reports = [];
    private DateTimeOffset _applicationStartTime = DateTimeOffset.Now;

    private class FixedReport(HealthCheckEntity result) : IServiceHealthReport
    {
        public Task<HealthCheckEntity> RunAsync(CancellationToken? cancellationToken = null) => Task.FromResult(result);
    }

    private class StubBuildMetadata : IBuildMetadata
    {
        public string ApplicationName => "Application";
        public string ApplicationVersion => "1.0.0";
        public string TargetFramework => ".NETCoreApp,Version=v6.0";
        public string BuildNumber => "1.0.0-dev-9c0ff94421b3171045b7dca1ba180dbbe0cb6435";
        public string BuildMachineName => "Machine";
        public string BuildUsername => "username";
        public DateTimeOffset? BuildTimestamp { get; } = new DateTimeOffset(2017, 4, 10, 14, 51, 23, TimeSpan.FromHours(2));
        public string BuildCommitHash => "9c0ff94421b3171045b7dca1ba180dbbe0cb6435";
        public string RunbookUri => "http://some.com/runbook";
    }
}
