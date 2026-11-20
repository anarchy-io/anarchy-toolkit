using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Core.Health.Reports;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Health.Core.Health.Reports;

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
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reports = Enumerable.Range(0, 3).Select(_ => new BlockingReport(release.Task)).ToArray();
        var pending = HealthCheckReport.RunAsync(reports, TestContext.Current.CancellationToken);

        try
        {
            await Task.WhenAll(reports.Select(report => report.Started))
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            pending.IsCompleted.Should().BeFalse();
        }
        finally
        {
            release.TrySetResult();
        }

        var result = await pending;
        result.Tests.Should().HaveCount(3);
    }

    [Fact]
    public async Task RunAsyncWithNullReportsThrows()
    {
        var act = () => HealthCheckReport.RunAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private class FixedReport(string name, HealthCheckResult result) : IHealthCheck
    {
        public Task<HealthCheckOutcome> RunAsync(CancellationToken? cancellationToken = null) =>
            Task.FromResult(new HealthCheckOutcome(name, result, TimeSpan.Zero));
    }

    private class BlockingReport(Task release) : IHealthCheck
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Started => _started.Task;

        public async Task<HealthCheckOutcome> RunAsync(CancellationToken? cancellationToken = null)
        {
            _started.TrySetResult();
            await release.WaitAsync(cancellationToken ?? CancellationToken.None);
            return new HealthCheckOutcome("blocked", HealthCheckResult.Passed, TimeSpan.Zero);
        }
    }
}
