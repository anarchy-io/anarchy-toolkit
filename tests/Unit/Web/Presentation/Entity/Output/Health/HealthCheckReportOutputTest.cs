using Anarchy.Toolkit.Core.Health.Entity.Api.Output;
using Anarchy.Toolkit.Core.Health.Entity.Service;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Web.Presentation.Entity.Output.Health;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Anarchy.Toolkit.Tests.Unit.Web.Presentation.Entity.Output.Health;

public class HealthCheckReportOutputTest : TestCaseBase
{
    [Fact]
    public void FromDto_ProblemPresent_MapsNestedException()
    {
        var problem = new ExceptionReport(new InvalidOperationException("boom"));

        var test = new HealthCheckEntity(
            Name: "Database",
            Result: "failed",
            TestedAt: DateTimeOffset.UtcNow,
            Duration: TimeSpan.FromSeconds(1),
            Problem: problem
        );

        var report = new HealthCheckReport(false, TimeSpan.FromSeconds(2), [test]);

        var result = HealthCheckReportOutput.FromDto(report);

        using (new AssertionScope())
        {
            result.Passed.Should().BeFalse();
            result.Duration.Should().Be(TimeSpan.FromSeconds(2));
            result.Tests.Should().HaveCount(1);

            var mappedTest = result.Tests.Single();
            mappedTest.Name.Should().Be("Database");
            mappedTest.Result.Should().Be("failed");
            mappedTest.Duration.Should().Be(TimeSpan.FromSeconds(1));
            mappedTest.Problem.Should().NotBeNull();
            mappedTest.Problem!.FinalException.Should().Be("boom");
            mappedTest.Problem.OriginalException.Should().Be("boom");
            mappedTest.Problem.Stack.Should().NotBeEmpty();
        }
    }

    [Fact]
    public void FromDto_ProblemAbsent_MapsNullProblem()
    {
        var test = new HealthCheckEntity(
            Name: "Cache",
            Result: "passed",
            TestedAt: DateTimeOffset.UtcNow,
            Duration: TimeSpan.FromMilliseconds(500),
            Problem: null
        );

        var report = new HealthCheckReport(true, TimeSpan.FromMilliseconds(500), [test]);

        var result = HealthCheckReportOutput.FromDto(report);

        result.Tests.Single().Problem.Should().BeNull();
    }
}
