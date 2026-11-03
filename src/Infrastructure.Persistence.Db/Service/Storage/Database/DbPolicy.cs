using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Database policy to handle database operations. The operation will be executed in a provided callback function.
///     On execute it will retrieve a new connection from the pool. When used in combination with a unit of work session,
///     the active connection will be retrieved from the <c>IAsyncFlowContext</c>.
/// </summary>
public class DbPolicy<TDbConnection>(
    IDbConnectionPool<TDbConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<TDbConnection>> asyncFlowContext,
    Observer? observer = null)
    : DbPolicyBase<TDbConnection>(dbConnectionPool, asyncFlowContext, observer)
    where TDbConnection : DbConnection
{
    protected override async Task<TDbResult> ExecuteCallbackAsync<TDbResult>(
        StoragePolicyCallback<TDbConnection, TDbResult> callback,
        StorageConnectionEnvelopeBase<TDbConnection> connection,
        CancellationToken? cancellationToken = null
    )
    {
        return await callback(connection, cancellationToken);
    }
}
