using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Core.Health.Core.Service.Health;

public class ServiceHealthReportBaseTests
{
    [Fact]
    public async Task RunAsyncReturnsTheReportName()
    {
        var result = await new TestReport().RunAsync(TestContext.Current.CancellationToken);

        result.Name.Should().Be("Health test");
    }

    [Fact]
    public async Task RunAsyncPassesWhenTheCheckReturns()
    {
        var result = await new TestReport().RunAsync(TestContext.Current.CancellationToken);

        result.Result.Should().Be(HealthCheckResult.Passed.ToDescription());
        result.Problem.Should().BeNull();
    }

    [Fact]
    public async Task RunAsyncReturnsTheCheckDuration()
    {
        var sut = new TestReport { Delay = TimeSpan.FromMilliseconds(500) };

        var result = await sut.RunAsync(TestContext.Current.CancellationToken);

        result.Duration.Should().BeCloseTo(TimeSpan.FromMilliseconds(500), precision: TimeSpan.FromMilliseconds(150));
    }

    [Fact]
    public async Task RunAsyncFailsWithTheProblemWhenTheCheckThrows()
    {
        var sut = new TestReport { ExceptionToThrow = new InvalidOperationException("unreachable") };

        var result = await sut.RunAsync(TestContext.Current.CancellationToken);

        result.Result.Should().Be(HealthCheckResult.Failed.ToDescription());
        result.Problem!.FinalException.Should().Be("unreachable");
    }

    [Fact]
    public async Task RunAsyncRethrowsWhenTheCallerCancels()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var sut = new TestReport { Delay = TimeSpan.FromSeconds(5) };

        var act = () => sut.RunAsync(cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RunAsyncFailsWhenTheCheckCancelsOnItsOwn()
    {
        var sut = new TestReport { ExceptionToThrow = new TaskCanceledException("timed out") };

        var result = await sut.RunAsync(TestContext.Current.CancellationToken);

        result.Result.Should().Be(HealthCheckResult.Failed.ToDescription());
    }

    private class TestReport : ServiceHealthReportBase
    {
        public TimeSpan Delay { get; init; } = TimeSpan.Zero;
        public Exception? ExceptionToThrow { get; init; }

        protected override string Name => "Health test";

        protected override async Task CheckAsync(CancellationToken cancellationToken)
        {
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            await Task.Delay(Delay, cancellationToken);
        }
    }
}
