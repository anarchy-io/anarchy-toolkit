using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Service.Storage.Database;

public class DbAsyncRetryPolicyBuilderTest
{
    private DbAsyncRetryPolicyBuilder<DummyDbConnection, DummyDbDialect> Sut() =>
        new(new DbResilienceOptions(RetryCount: 1));

    [Fact]
    public async Task Build_RetriesExceptionsTheDialectClassifiesAsTransient()
    {
        var attempts = 0;

        var result = await Sut().Build().ExecuteAsync(_ => {
            attempts++;
            return attempts == 1
                ? throw new InvalidOperationException("transient")
                : ValueTask.FromResult(attempts);
        }, TestContext.Current.CancellationToken);

        result.Should().Be(2);
    }

    [Fact]
    public async Task Build_DoesNotRetryNonTransientExceptions()
    {
        var attempts = 0;

        var act = async () => await Sut().Build().ExecuteAsync(_ => {
            attempts++;
            return ValueTask.FromException(new TimeoutException("not transient for this dialect"));
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<TimeoutException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task Build_WhenRetrying_LogsThroughTheConsumerSink()
    {
        var token = TestContext.Current.CancellationToken;
        var sink = new InMemorySink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddSerilogTelemetry(logger);
        using var provider = services.BuildServiceProvider();
        var telemetry = provider.GetRequiredService<IAppTelemetry>();
        var attempts = 0;
        var pipeline = new DbAsyncRetryPolicyBuilder<DummyDbConnection, DummyDbDialect>(
            new DbResilienceOptions(RetryCount: 1), telemetry).Build();

        var result = await pipeline.ExecuteAsync(_ => {
            attempts++;
            return attempts == 1
                ? throw new InvalidOperationException("transient")
                : ValueTask.FromResult(attempts);
        }, token);

        result.Should().Be(2);
        sink.Messages.Should().Contain(entry => entry.RenderMessage().Contains("Retry operation attempt 1"));
    }
}
