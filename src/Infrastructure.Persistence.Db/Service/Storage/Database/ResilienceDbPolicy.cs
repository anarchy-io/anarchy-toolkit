using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Polly;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Database policy to handle database operations with a resilience (retry) pipeline.
///     On execute it will retrieve a new connection from the pool. When used in combination with a unit of work session,
///     the active connection will be retrieved from the <c>IAsyncFlowContext</c>.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <param name="retryPolicyBuilder">Builder of the retry pipeline; it is built once, in the constructor.</param>
/// <param name="dbConnectionPool">Connection pool used when no unit of work session is active.</param>
/// <param name="asyncFlowContext">Async flow context holding the connection of the active unit of work session, if any.</param>
/// <param name="telemetry">Optional application telemetry used for logging and metrics.</param>
/// <remarks>
///     Retries reuse the same connection, which is acquired once per <see cref="DbPolicyBase{TDbConnection}.ExecuteAsync{TDbResult}" />
///     call. Inside a transaction a retried statement runs in the same (possibly aborted) transaction, so callbacks
///     should be safe to repeat.
/// </remarks>
/// <example>
///     <code language="csharp">
///     var policy = new ResilienceDbPolicy&lt;NpgsqlConnection&gt;(
///         new DbAsyncRetryPolicyBuilder&lt;NpgsqlConnection, PostgresDbDialect&gt;(new DbResilienceOptions(3), telemetry),
///         connectionPool,
///         new AsyncLocalFlowContext&lt;StorageConnectionEnvelopeBase&lt;NpgsqlConnection&gt;&gt;(),
///         telemetry);
///
///     var now = await policy.ExecuteAsync(
///         (envelope, ct) => envelope.DataStorageConnection.QueryFirstAsync&lt;DateTime&gt;("SELECT now()"),
///         "Clock.Get");
///     </code>
/// </example>
public class ResilienceDbPolicy<TDbConnection>(
    IAsyncRetryPolicyBuilder<TDbConnection> retryPolicyBuilder,
    IDbConnectionPool<TDbConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<TDbConnection>> asyncFlowContext,
    IAppTelemetry? telemetry = null)
    : DbPolicyBase<TDbConnection>(dbConnectionPool, asyncFlowContext, telemetry)
    where TDbConnection : DbConnection
{
    private readonly ResiliencePipeline _retryPolicy = retryPolicyBuilder.Build();

    /// <summary>Invokes <paramref name="callback" /> on <paramref name="connection" /> through the retry pipeline.</summary>
    /// <typeparam name="TDbResult">Type of the callback result.</typeparam>
    /// <param name="callback">The callback to execute.</param>
    /// <param name="connection">The connection to execute on.</param>
    /// <param name="cancellationToken">Token passed to the pipeline, which forwards its own token to the callback.</param>
    /// <returns>The callback result.</returns>
    protected override async Task<TDbResult> ExecuteCallbackAsync<TDbResult>(
        StoragePolicyCallback<TDbConnection, TDbResult> callback,
        StorageConnectionEnvelopeBase<TDbConnection> connection,
        CancellationToken? cancellationToken = null
    )
    {
        return await _retryPolicy.ExecuteAsync(
            async ct => await callback(connection, ct),
            cancellationToken ?? CancellationToken.None
        );
    }
}
