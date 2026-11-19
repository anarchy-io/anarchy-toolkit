using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Telemetry.Serilog;
using Anarchy.Toolkit.Testing.Helper.Observability.Monitoring;
using Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Service.Storage.Database;

public class DbPolicyBaseTest
{
    private sealed class DummyConnectionPool() : DbConnectionPoolBase<DummyDbConnection>(DataStorageInstanceType.Reader, "Data Source=dummy")
    {
        protected override DummyDbConnection CreateConnection(string connectionString) => new(connectionString);
    }

    [Fact]
    public async Task ExecuteAsync_EmitsLogsAndMetricsToTheConsumerSink()
    {
        var token = TestContext.Current.CancellationToken;
        var sink = new InMemorySink();
        using var logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Sink(sink).CreateLogger();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
        services.AddMetrics();
        services.AddSerilogTelemetry(logger);
        services.AddTelemetryLogMetrics();
        using var provider = services.BuildServiceProvider();
        var listener = provider.GetServices<IHostedService>().Should().ContainSingle().Which;
        await listener.StartAsync(token);
        var telemetry = provider.GetRequiredService<IAppTelemetry>();
        var sut = new DbPolicy<DummyDbConnection>(
            new DummyConnectionPool(),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<DummyDbConnection>>(),
            telemetry);

        var result = await sut.ExecuteAsync((_, _) => Task.FromResult(true), operationName: "Orders.Load", cancellationToken: token);

        result.Should().BeTrue();
        sink.Messages.Should().Contain(entry => entry.RenderMessage().Contains("Finished policy execution"));
        sink.Messages.Should().Contain(entry => entry.RenderMessage().Contains("execution_time"));
        sink.Messages.Should().Contain(entry => entry.RenderMessage().Contains("Orders.Load"));
        await listener.StopAsync(token);
    }
}
