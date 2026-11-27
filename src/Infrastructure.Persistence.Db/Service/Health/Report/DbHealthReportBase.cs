using Anarchy.Toolkit.Core.Extensions.System;
using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.System;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Extensions.Microsoft.Extensions.DependencyInjection;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Diagnostics;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report;

/// <summary>
///     Base health report that verifies connectivity to a database instance by executing
///     <see cref="IDbDialect.HealthCheckSql" /> over the supplied storage policy. The test is named
///     <c>{dialect}_connection-{instance type}</c>, for example <c>postgres_connection-reader</c>.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <typeparam name="TDialect">The provider dialect supplying the health check SQL and name.</typeparam>
/// <param name="dbPolicy">Policy used to execute the health check query; should target the instance being checked.</param>
/// <param name="telemetry">Application telemetry used to log failures.</param>
/// <param name="dbInstanceType">The instance type (reader or writer) being checked, used in the test name.</param>
/// <remarks>
///     Failures never throw: the exception is logged and returned in a failed <see cref="HealthCheckOutcome" />.
///     Register reader and writer subclasses with
///     <see cref="DbHealthServiceCollectionExtensions.AddDbHealthReports{TReaderReport,TWriterReport}" />.
/// </remarks>
public abstract class DbHealthReportBase<TDbConnection, TDialect>(
    IStoragePolicy<TDbConnection> dbPolicy,
    AppTelemetry telemetry,
    DataStorageInstanceType dbInstanceType) : IHealthCheck
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
    /// <inheritdoc />
    public async Task<HealthCheckOutcome> RunAsync(CancellationToken? cancellationToken = null)
    {
        var caller = this.CalledFrom();
        var timer = Stopwatch.StartNew();
        var testName = $"{TDialect.Name}_connection-{dbInstanceType.ToDescription()}";

        try
        {
            await dbPolicy.ExecuteAsync(
                async (envelope, _) =>
                    await envelope.DataStorageConnection.QueryFirstAsync<DateTime>(new CommandDefinition(
                        commandText: TDialect.HealthCheckSql,
                        cancellationToken: cancellationToken ?? CancellationToken.None)),
                cancellationToken: cancellationToken);

            return new HealthCheckOutcome(testName, HealthCheckResult.Passed, timer.Elapsed);
        }
        catch (Exception exception)
        {
            telemetry.Log(LogLevel.Error, caller, exception, "Service health test failed for {ServiceName}", testName);
            return new HealthCheckOutcome(testName, HealthCheckResult.Failed, timer.Elapsed, exception);
        }
    }
}
