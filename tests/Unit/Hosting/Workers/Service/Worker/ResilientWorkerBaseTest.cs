using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Hosting.Workers.Service.Worker;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Entity;
using Anarchy.Toolkit.Infrastructure.CorrelationId.Core.Service;
using Anarchy.Toolkit.Testing.Telemetry;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Service.Worker;

public sealed class ResilientWorkerBaseTest : IDisposable
{
    private const double IntervalInSeconds = 0.01; // 10 ms — kept tiny so the loop iterates quickly
    private readonly TestTelemetry _telemetry = new();

    private ResilientWorkerFixture CreateSut(ResilientWorkerFixture.PerformWorkResult result,
                                             CorrelationIdOptions? options = null) =>
        new(result, _telemetry.Telemetry, IntervalInSeconds, IntervalInSeconds, options);

    public void Dispose() => _telemetry.Dispose();

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
        _telemetry.Logs.Should().Contain(log => log.Message.Contains("Worker action failed; retrying"));
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_GivesEachIterationACorrelationIdAndEachAttemptAnAttemptId()
    {
        var sut = CreateSut(ResilientWorkerFixture.PerformWorkResult.FailsOnceThenTrue);

        await sut.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => sut.Attempts.Count >= 3);
        await sut.StopAsync(TestContext.Current.CancellationToken);

        var attempts = sut.Attempts.ToArray();
        attempts.Should().AllSatisfy(attempt => {
            attempt.CorrelationId.Should().NotBeNullOrEmpty();
            attempt.AttemptId.Should().NotBeNullOrEmpty();
        });
        attempts[0].CorrelationId.Should().Be(attempts[1].CorrelationId, "a retry belongs to the same iteration");
        attempts[0].AttemptId.Should().NotBe(attempts[1].AttemptId, "each attempt gets its own id");
        attempts[2].CorrelationId.Should().NotBe(attempts[1].CorrelationId, "the next iteration gets a new id");
        _telemetry.Logs.Should().Contain(log => log.Message.Contains("Working")
                                                && Equals(log.Scopes.GetValueOrDefault("CorrelationId"), attempts[0].CorrelationId)
                                                && Equals(log.Scopes.GetValueOrDefault("AttemptId"), attempts[0].AttemptId),
            "the ids reach the logging scope");
        _telemetry.Logs.Should().Contain(log => log.Message.Contains("Worker action failed; retrying")
                                                && Equals(log.Scopes.GetValueOrDefault("CorrelationId"), attempts[0].CorrelationId));
        CorrelationContext.CorrelationId.Should().BeNull("the scope is restored once the iteration ends");
    }

    [Fact(Timeout = 5000)]
    public async Task ExecuteAsync_UsesTheConfiguredCorrelationPropertyNames()
    {
        var options = new CorrelationIdOptions { CorrelationIdPropertyName = "trace.correlation", AttemptIdPropertyName = "trace.attempt" };
        var sut = CreateSut(ResilientWorkerFixture.PerformWorkResult.True, options);

        await sut.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => sut.Attempts.Count >= 1);
        await sut.StopAsync(TestContext.Current.CancellationToken);

        _telemetry.Logs.Should().Contain(log => log.Message.Contains("Working")
                                                && log.Scopes.ContainsKey("trace.correlation")
                                                && log.Scopes.ContainsKey("trace.attempt"));
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
        AppTelemetry telemetry,
        double recurringIntervalInSeconds,
        double delayedIntervalInSeconds,
        CorrelationIdOptions? correlationIdOptions)
        : ResilientWorkerBase(telemetry, recurringIntervalInSeconds, delayedIntervalInSeconds,
            correlationIdOptions: correlationIdOptions)
    {
        private int _performWorkCallCount;

        public int PerformWorkCallCount => _performWorkCallCount;

        public System.Collections.Concurrent.ConcurrentQueue<(string? CorrelationId, string? AttemptId)> Attempts { get; } = new();

        public enum PerformWorkResult
        {
            True,
            False,
            Exception,
            BlocksUntilCancelled,
            FailsOnceThenTrue
        }

        protected override async Task<bool> PerformWorkAsync(CancellationToken token)
        {
            var call = Interlocked.Increment(ref _performWorkCallCount);
            Telemetry.LogInformation(this.CalledFrom(), "Working");
            Attempts.Enqueue((CorrelationContext.CorrelationId, CorrelationContext.AttemptId));

            if (resultPerformWork == PerformWorkResult.BlocksUntilCancelled)
            {
                await Task.Delay(int.MaxValue, token); // blocks until token is cancelled
                return false;
            }

            return resultPerformWork switch {
                PerformWorkResult.True => true,
                PerformWorkResult.False => false,
                PerformWorkResult.Exception => throw new Exception("Mocked behavior"),
                PerformWorkResult.FailsOnceThenTrue when call % 2 == 1 => throw new Exception("Transient"),
                PerformWorkResult.FailsOnceThenTrue => true,
                _ => throw new ArgumentOutOfRangeException(nameof(PerformWorkResult), resultPerformWork, null)
            };
        }
    }
}
