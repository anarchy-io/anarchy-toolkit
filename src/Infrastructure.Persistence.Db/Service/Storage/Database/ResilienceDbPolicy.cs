using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Polly;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Database policy to handle database operations with a resilience (retry) pipeline.
///     On execute it will retrieve a new connection from the pool. When used in combination with a unit of work session,
///     the active connection will be retrieved from the <c>IAsyncFlowContext</c>.
/// </summary>
public class ResilienceDbPolicy<TDbConnection>(
    IAsyncRetryPolicyBuilder<TDbConnection> retryPolicyBuilder,
    IDbConnectionPool<TDbConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<TDbConnection>> asyncFlowContext,
    Observer? observer = null)
    : DbPolicyBase<TDbConnection>(dbConnectionPool, asyncFlowContext, observer)
    where TDbConnection : DbConnection
{
    private readonly ResiliencePipeline _retryPolicy = retryPolicyBuilder.Build();

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
