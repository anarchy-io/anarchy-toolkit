using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Entity.Api.Output;
using Anarchy.Toolkit.Core.Health.Entity.Service;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Health.Core.Entity.Api.Output;

public class HealthCheckReportTests
{
    [Fact]
    public async Task RunAsyncWithoutReportsPasses()
    {
        var report = await HealthCheckReport.RunAsync([], TestContext.Current.CancellationToken);

        report.Passed.Should().BeTrue();
        report.Tests.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsyncReturnsOneTestPerReportInOrder()
    {
        var report = await HealthCheckReport.RunAsync(
            [new FixedReport("first", HealthCheckResult.Passed), new FixedReport("second", HealthCheckResult.NotRun)],
            TestContext.Current.CancellationToken);

        report.Passed.Should().BeTrue();
        report.Tests.Select(test => test.Name).Should().Equal("first", "second");
    }

    [Fact]
    public async Task RunAsyncFailsWhenAnyReportFails()
    {
        var report = await HealthCheckReport.RunAsync(
            [new FixedReport("first", HealthCheckResult.Passed), new FixedReport("second", HealthCheckResult.Failed)],
            TestContext.Current.CancellationToken);

        report.Passed.Should().BeFalse();
        report.Tests.Single(test => test.Name == "second").Result
              .Should().Be(HealthCheckResult.Failed.ToDescription());
    }

    [Fact]
    public async Task RunAsyncRunsReportsInParallel()
    {
        var delay = TimeSpan.FromMilliseconds(300);

        var report = await HealthCheckReport.RunAsync(
            [new DelayedReport(delay), new DelayedReport(delay), new DelayedReport(delay)],
            TestContext.Current.CancellationToken);

        report.Duration.Should().BeLessThan(delay * 2);
    }

    [Fact]
    public async Task RunAsyncWithNullReportsThrows()
    {
        var act = () => HealthCheckReport.RunAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private class FixedReport(string name, HealthCheckResult result) : IServiceHealthReport
    {
        public Task<HealthCheckEntity> RunAsync(CancellationToken? cancellationToken = null) =>
            Task.FromResult(new HealthCheckEntity(name, result, TimeSpan.Zero));
    }

    private class DelayedReport(TimeSpan delay) : IServiceHealthReport
    {
        public async Task<HealthCheckEntity> RunAsync(CancellationToken? cancellationToken = null)
        {
            await Task.Delay(delay, cancellationToken ?? CancellationToken.None);
            return new HealthCheckEntity("delayed", HealthCheckResult.Passed, delay);
        }
    }
}
