using Anarchy.Toolkit.Core.Extensions.System;
using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Health.Report.Postgres;
using AwesomeAssertions;
using Moq;
using Npgsql;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Postgres.Service.Health.Report.Postgres;

public class PostgresHealthReportWriterTest
{
    private PostgresHealthReportWriter Sut(bool success = true)
    {
        var mockPolicy = new Mock<IStoragePolicy<NpgsqlConnection>>();

        if (!success)
            mockPolicy.Setup(x => x.ExecuteAsync(It.IsAny<StoragePolicyCallback<NpgsqlConnection, DateTime>>(),
                    It.IsAny<string?>(), It.IsAny<CancellationToken?>()))
                .ThrowsAsync(new Exception("Mocked Behavior"));

        return new PostgresHealthReportWriter(mockPolicy.Object, AppTelemetry.Null);
    }

    [Fact]
    public async Task RunAsync_WhenConnectionIsAvailable_ReturnsPassed()
    {
        var report = await Sut().RunAsync();
        report.Name.Should().Be("postgres_connection-writer");
        report.Result.Should().Be(HealthCheckResult.Passed.ToDescription());
    }

    [Fact]
    public async Task RunAsync_WhenConnectionIsNotAvailable_ReturnsFailed()
    {
        var report = await Sut(false).RunAsync();
        report.Name.Should().Be("postgres_connection-writer");
        report.Result.Should().Be(HealthCheckResult.Failed.ToDescription());
    }
}
