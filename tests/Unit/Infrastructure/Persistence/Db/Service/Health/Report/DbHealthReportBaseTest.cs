using Anarchy.Toolkit.Core.Extensions.System;
using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Data.Common;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Service.Health.Report;

public class DbHealthReportBaseTest
{
    private sealed class DummyDialect : IDbDialect
    {
        public static string Name => "dummy";
        public static bool IsNoData(Exception exception) => false;
        public static bool IsUniqueViolation(Exception exception) => false;
        public static string HealthCheckSql => "SELECT 1";
    }

    private sealed class DummyHealthReport(
        IStoragePolicy<DbConnection> dbPolicy,
        AppTelemetry telemetry,
        DataStorageInstanceType instanceType)
        : DbHealthReportBase<DbConnection, DummyDialect>(dbPolicy, telemetry, instanceType);

    private sealed class ReaderReport(IStoragePolicy<DbConnection> dbPolicy, AppTelemetry telemetry)
        : DbHealthReportBase<DbConnection, DummyDialect>(dbPolicy, telemetry, DataStorageInstanceType.Reader);

    private sealed class WriterReport(IStoragePolicy<DbConnection> dbPolicy, AppTelemetry telemetry)
        : DbHealthReportBase<DbConnection, DummyDialect>(dbPolicy, telemetry, DataStorageInstanceType.Writer);

    private DummyHealthReport Sut(DataStorageInstanceType instanceType, bool success = true)
    {
        var mockPolicy = new Mock<IStoragePolicy<DbConnection>>();

        if (!success)
            mockPolicy.Setup(x => x.ExecuteAsync(It.IsAny<StoragePolicyCallback<DbConnection, DateTime>>(),
                    It.IsAny<string?>(), It.IsAny<CancellationToken?>()))
                .ThrowsAsync(new Exception("Mocked Behavior"));

        return new DummyHealthReport(mockPolicy.Object, AppTelemetry.Null, instanceType);
    }

    [Theory]
    [InlineData(DataStorageInstanceType.Reader, "dummy_connection-reader")]
    [InlineData(DataStorageInstanceType.Writer, "dummy_connection-writer")]
    public async Task RunAsync_WhenConnectionIsAvailable_ReturnsPassedNamedAfterDialect(
        DataStorageInstanceType instanceType, string expectedName)
    {
        var report = await Sut(instanceType).RunAsync(TestContext.Current.CancellationToken);

        report.Name.Should().Be(expectedName);
        report.Result.Should().Be(HealthCheckResult.Passed.ToDescription());
    }

    [Fact]
    public async Task RunAsync_WhenConnectionIsNotAvailable_ReturnsFailed()
    {
        var report = await Sut(DataStorageInstanceType.Reader, success: false)
            .RunAsync(TestContext.Current.CancellationToken);

        report.Name.Should().Be("dummy_connection-reader");
        report.Result.Should().Be(HealthCheckResult.Failed.ToDescription());
    }

    [Fact]
    public void AddDbHealthReports_ExposesReaderAndWriterReports()
    {
        var provider = new ServiceCollection()
            .AddSingleton(new Mock<IStoragePolicy<DbConnection>>().Object)
            .AddSingleton(AppTelemetry.Null)
            .AddDbHealthReports<ReaderReport, WriterReport>()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IReadOnlyCollection<IHealthCheck>>()
            .Should().SatisfyRespectively(
                first => first.Should().BeOfType<ReaderReport>(),
                second => second.Should().BeOfType<WriterReport>());
    }
}
