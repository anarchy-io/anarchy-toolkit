using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Entity.Service;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Dapper;
using System.Data.Common;
using System.Diagnostics;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Health.Report;

/// <summary>
///     Base health report that verifies connectivity to a database instance by executing
///     <see cref="IDbDialect.HealthCheckSql" /> over the supplied storage policy. The test is named
///     <c>{dialect}_connection-{instance type}</c>, for example <c>postgres_connection-reader</c>.
/// </summary>
public abstract class DbHealthReportBase<TDbConnection, TDialect>(
    IStoragePolicy<TDbConnection> dbPolicy,
    Observer observer,
    DataStorageInstanceType dbInstanceType) : IServiceHealthReport
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
    /// <inheritdoc />
    public async Task<HealthCheckEntity> RunAsync(CancellationToken? cancellationToken = null)
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

            return new HealthCheckEntity(testName, HealthCheckResult.Passed, timer.Elapsed);
        }
        catch (Exception exception)
        {
            observer.LogError(exception, (CallSiteEntity)caller, "Service health test failed for {ServiceName}", testName);
            return new HealthCheckEntity(testName, HealthCheckResult.Failed, timer.Elapsed, exception);
        }
    }
}
