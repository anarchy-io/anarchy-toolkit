using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql;

/// <summary>
/// MySQL flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.ResilienceDbPolicy{TDbConnection}"/>: executes database
/// callbacks with retries, using the active unit-of-work connection when present or a pooled one otherwise.
/// </summary>
/// <remarks>
/// <c>ModuleMysql</c> registers one instance per instance type, keyed <c>"writer"</c> and <c>"reader"</c>, as
/// <c>IStoragePolicy&lt;MySqlConnection&gt;</c>; the unkeyed registration resolves to the writer.
/// Unlike the base policy, callbacks running inside a transaction are <b>not</b> retried: MySQL may roll back the whole
/// transaction on a deadlock, so retrying a single command would run it outside the intended transaction. Retry the
/// whole unit of work instead.
/// </remarks>
/// <param name="retryPolicyBuilder">Builder of the retry pipeline.</param>
/// <param name="dbConnectionPool">Connection pool used when no unit of work session is active.</param>
/// <param name="asyncFlowContext">Async flow context holding the connection of the active unit of work session, if any.</param>
/// <param name="telemetry">Optional application telemetry used for logging and metrics.</param>
public class MysqlResilienceDbPolicy(
    IAsyncRetryPolicyBuilder<MySqlConnection> retryPolicyBuilder,
    IDbConnectionPool<MySqlConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>> asyncFlowContext,
    IAppTelemetry? telemetry = null)
    : ResilienceDbPolicy<MySqlConnection>(retryPolicyBuilder, dbConnectionPool, asyncFlowContext, telemetry)
{
    // MySQL can roll back the entire transaction on a deadlock. Retrying an individual command would then
    // run outside the intended transaction; let the caller retry the whole unit of work instead.
    /// <inheritdoc/>
    /// <remarks>Bypasses the retry pipeline when the connection is in a transaction.</remarks>
    protected override Task<TResult> ExecuteCallbackAsync<TResult>(
        StoragePolicyCallback<MySqlConnection, TResult> callback,
        StorageConnectionEnvelopeBase<MySqlConnection> connection,
        CancellationToken? cancellationToken = null) =>
        connection.IsInTransaction
            ? callback(connection, cancellationToken)
            : base.ExecuteCallbackAsync(callback, connection, cancellationToken);
}
