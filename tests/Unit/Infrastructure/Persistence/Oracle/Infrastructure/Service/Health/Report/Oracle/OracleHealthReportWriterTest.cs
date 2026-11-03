using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Health.Report.Oracle;
using Anarchy.Toolkit.Testing;
using AwesomeAssertions;
using Moq;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Oracle.Infrastructure.Service.Health.Report.Oracle;

public class OracleHealthReportWriterTest : TestCaseBase
{
    private OracleHealthReportWriter Sut(bool success = true)
    {
        var mockPolicy = new Mock<IStoragePolicy<OracleConnection>>();

        if (!success)
            mockPolicy.Setup(x => x.ExecuteAsync(It.IsAny<StoragePolicyCallback<OracleConnection, DateTime>>(),
                    It.IsAny<string?>(), It.IsAny<CancellationToken?>()))
                .ThrowsAsync(new Exception("Mocked Behavior"));

        return new OracleHealthReportWriter(mockPolicy.Object, FixtureObserver);
    }

    [Fact]
    public async Task RunAsync_WhenConnectionIsAvailable_ReturnsPassed()
    {
        var report = await Sut().RunAsync();
        report.Name.Should().Be("oracle_connection-writer");
        report.Result.Should().Be(HealthCheckResult.Passed.ToDescription());
    }

    [Fact]
    public async Task RunAsync_WhenConnectionIsNotAvailable_ReturnsFailed()
    {
        var report = await Sut(false).RunAsync();
        report.Name.Should().Be("oracle_connection-writer");
        report.Result.Should().Be(HealthCheckResult.Failed.ToDescription());
    }
}
