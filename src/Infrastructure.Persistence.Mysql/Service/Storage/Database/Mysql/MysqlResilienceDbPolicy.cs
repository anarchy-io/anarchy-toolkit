using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;

public class MysqlResilienceDbPolicy(
    IAsyncRetryPolicyBuilder<MySqlConnection> retryPolicyBuilder,
    IDbConnectionPool<MySqlConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>> asyncFlowContext,
    Observer? observer = null)
    : ResilienceDbPolicy<MySqlConnection>(retryPolicyBuilder, dbConnectionPool, asyncFlowContext, observer)
{
    // MySQL can roll back the entire transaction on a deadlock. Retrying an individual command would then
    // run outside the intended transaction; let the caller retry the whole unit of work instead.
    protected override Task<TResult> ExecuteCallbackAsync<TResult>(
        StoragePolicyCallback<MySqlConnection, TResult> callback,
        StorageConnectionEnvelopeBase<MySqlConnection> connection,
        CancellationToken? cancellationToken = null) =>
        connection.IsInTransaction
            ? callback(connection, cancellationToken)
            : base.ExecuteCallbackAsync(callback, connection, cancellationToken);
}
