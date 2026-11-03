using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Health.Report.Postgres;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;
using Moq;
using Npgsql;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Postgres.Infrastructure.Service.Health.Report.Postgres;

public class PostgresHealthReportWriterTest : TestCaseBase
{
    private PostgresHealthReportWriter Sut(bool success = true)
    {
        var mockPolicy = new Mock<IStoragePolicy<NpgsqlConnection>>();

        if (!success)
            mockPolicy.Setup(x => x.ExecuteAsync(It.IsAny<StoragePolicyCallback<NpgsqlConnection, DateTime>>(),
                    It.IsAny<string?>(), It.IsAny<CancellationToken?>()))
                .ThrowsAsync(new Exception("Mocked Behavior"));

        return new PostgresHealthReportWriter(mockPolicy.Object, FixtureObserver);
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
