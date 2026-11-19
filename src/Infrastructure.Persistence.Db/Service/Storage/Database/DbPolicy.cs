using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Database policy to handle database operations. The operation will be executed in a provided callback function.
///     On execute it will retrieve a new connection from the pool. When used in combination with a unit of work session,
///     the active connection will be retrieved from the <c>IAsyncFlowContext</c>.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <param name="dbConnectionPool">Connection pool used when no unit of work session is active.</param>
/// <param name="asyncFlowContext">Async flow context holding the connection of the active unit of work session, if any.</param>
/// <param name="telemetry">Optional application telemetry used for logging and metrics.</param>
/// <remarks>
///     Executes the callback once, without retries. Use <see cref="ResilienceDbPolicy{TDbConnection}" /> to retry
///     transient failures.
/// </remarks>
public class DbPolicy<TDbConnection>(
    IDbConnectionPool<TDbConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<TDbConnection>> asyncFlowContext,
    IAppTelemetry? telemetry = null)
    : DbPolicyBase<TDbConnection>(dbConnectionPool, asyncFlowContext, telemetry)
    where TDbConnection : DbConnection
{
    /// <summary>Invokes <paramref name="callback" /> once on <paramref name="connection" />.</summary>
    /// <typeparam name="TDbResult">Type of the callback result.</typeparam>
    /// <param name="callback">The callback to execute.</param>
    /// <param name="connection">The connection to execute on.</param>
    /// <param name="cancellationToken">Token forwarded to the callback.</param>
    /// <returns>The callback result.</returns>
    protected override async Task<TDbResult> ExecuteCallbackAsync<TDbResult>(
        StoragePolicyCallback<TDbConnection, TDbResult> callback,
        StorageConnectionEnvelopeBase<TDbConnection> connection,
        CancellationToken? cancellationToken = null
    )
    {
        return await callback(connection, cancellationToken);
    }
}
