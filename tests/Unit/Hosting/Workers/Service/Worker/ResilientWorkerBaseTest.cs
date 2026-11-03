using Anarchy.Toolkit.Hosting.Workers.Service.Worker;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Testing.Helper;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Service.Worker;

public class ResilientWorkerBaseTest : TestCaseBase
{
    private const double IntervalInSeconds = 0.01; // 10 ms — kept tiny so the loop iterates quickly

    private ResilientWorkerFixture CreateSut(ResilientWorkerFixture.PerformWorkResult result) =>
        new(result, FixtureObserver, IntervalInSeconds, IntervalInSeconds);

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_WhenPerformWorkReturnsTrue_LoopsMultipleTimes()
    {
        var sut = CreateSut(ResilientWorkerFixture.PerformWorkResult.True);

        await sut.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => sut.PerformWorkCallCount >= 3);
        await sut.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(sut.Finished);
        Assert.True(sut.PerformWorkCallCount >= 3);
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_WhenPerformWorkReturnsFalse_LoopsMultipleTimes()
    {
        var sut = CreateSut(ResilientWorkerFixture.PerformWorkResult.False);

        await sut.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => sut.PerformWorkCallCount >= 3);
        await sut.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(sut.Finished);
        Assert.True(sut.PerformWorkCallCount >= 3);
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_WhenPerformWorkThrows_RetriesMultipleTimes()
    {
        var sut = CreateSut(ResilientWorkerFixture.PerformWorkResult.Exception);

        await sut.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => sut.PerformWorkCallCount >= 3, timeoutMs: 2000);
        await sut.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(sut.Finished);
        Assert.True(sut.PerformWorkCallCount >= 3);
        Assert.Contains(FixtureObserver.SinkLogs.Messages, x =>
            x.MessageTemplate.Text.Contains("Worker action failed; retrying"));
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_WhenCancellationRequestedDuringWork_ExitsGracefullyWithoutRetrying()
    {
        var sut = CreateSut(ResilientWorkerFixture.PerformWorkResult.BlocksUntilCancelled);

        await sut.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => sut.PerformWorkCallCount >= 1);
        await sut.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(sut.Finished);
        Assert.Equal(1, sut.PerformWorkCallCount); // token cancelled mid-work, no retry
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, int timeoutMs = 1000)
    {
        var timeoutAt = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < timeoutAt)
        {
            if (predicate())
                return;

            await Task.Delay(2, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException("Condition was not reached before timeout");
    }

    private class ResilientWorkerFixture(
        ResilientWorkerFixture.PerformWorkResult resultPerformWork,
        InMemoryObserver observer,
        double recurringIntervalInSeconds,
        double delayedIntervalInSeconds)
        : ResilientWorkerBase(observer, recurringIntervalInSeconds, delayedIntervalInSeconds)
    {
        private int _performWorkCallCount;

        public int PerformWorkCallCount => _performWorkCallCount;

        public enum PerformWorkResult
        {
            True,
            False,
            Exception,
            BlocksUntilCancelled
        }

        protected override async Task<bool> PerformWorkAsync(CancellationToken token)
        {
            Interlocked.Increment(ref _performWorkCallCount);

            if (resultPerformWork == PerformWorkResult.BlocksUntilCancelled)
            {
                await Task.Delay(int.MaxValue, token); // blocks until token is cancelled
                return false;
            }

            return resultPerformWork switch {
                PerformWorkResult.True => true,
                PerformWorkResult.False => false,
                PerformWorkResult.Exception => throw new Exception("Mocked behavior"),
                _ => throw new ArgumentOutOfRangeException(nameof(PerformWorkResult), resultPerformWork, null)
            };
        }
    }
}
