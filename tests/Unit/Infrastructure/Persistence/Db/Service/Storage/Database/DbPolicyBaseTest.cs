using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Testing.Telemetry;
using Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Service.Storage.Database;

public class DbPolicyBaseTest
{
    private sealed class DummyConnectionPool() : DbConnectionPoolBase<DummyDbConnection>(DataStorageInstanceType.Reader, "Data Source=dummy")
    {
        protected override DummyDbConnection CreateConnection(string connectionString) => new(connectionString);
    }

    [Fact]
    public async Task ExecuteAsync_EmitsLogsAndMetricsThroughTheInjectedTelemetry()
    {
        var token = TestContext.Current.CancellationToken;
        using var capture = new TestTelemetry();
        var telemetry = capture.Telemetry;
        var sut = new DbPolicy<DummyDbConnection>(
            new DummyConnectionPool(),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<DummyDbConnection>>(),
            telemetry);

        var result = await sut.ExecuteAsync((_, _) => Task.FromResult(true), operationName: "Orders.Load", cancellationToken: token);

        result.Should().BeTrue();
        capture.Logs.Should().Contain(log => log.Message.Contains("Finished policy execution"));
        capture.Metrics.Metrics.Should().Contain(metric => metric.Kind == MetricKind.Duration
                                                          && metric.Name.EndsWith(".execution_time")
                                                          && metric.HasTag("operation_name", "Orders.Load")
                                                          && metric.HasTag("success", true));
    }
}
