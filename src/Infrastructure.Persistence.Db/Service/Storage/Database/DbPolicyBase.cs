using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.Exceptions;
using System.Data.Common;
using System.Diagnostics;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Generic abstract implementation of a database policy to handle database operations.
///     The operation will be executed in a provided callback function.
///     On execute it will retrieve a new connection from the pool. When used in combination with a unit of work session,
///     the active connection will be retrieved from the <c>IAsyncFlowContext</c>.
/// </summary>
public abstract class DbPolicyBase<TDbConnection> : IStoragePolicy<TDbConnection>
    where TDbConnection : DbConnection
{
    /// <summary>
    ///     <c>IAsyncFlowContext</c> that contains active database connections within a unit of work session
    /// </summary>
    protected readonly IAsyncFlowContext<StorageConnectionEnvelopeBase<TDbConnection>> ActiveConnectionContext;

    /// <summary>
    ///     Connection pool of the database
    /// </summary>
    protected readonly IDbConnectionPool<TDbConnection> DbConnectionPool;

    /// <summary>
    ///     Observer used for logging and metrics
    /// </summary>
    protected readonly Observer? Observer;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="dbConnectionPool">Connection pool of the database</param>
    /// <param name="asyncFlowContext">
    ///     <c>IAsyncFlowContext</c> that contains active database connections within a unit of work
    ///     session
    /// </param>
    /// <param name="observer">Logger + MeasurerBase facade</param>
    protected DbPolicyBase(
        IDbConnectionPool<TDbConnection> dbConnectionPool,
        IAsyncFlowContext<StorageConnectionEnvelopeBase<TDbConnection>> asyncFlowContext,
        Observer? observer = null
    )
    {
        DbConnectionPool = dbConnectionPool;
        ActiveConnectionContext = asyncFlowContext;
        Observer = observer;
    }

    /// <summary>
    ///     Execute a database operation in a custom function provided by the user. The callback function will return
    ///     the active connection where database operation can be executed on.
    ///     When used in combination with a unit of work the active connection will be retrieved from the
    ///     <c>IAsyncFlowContext</c> else a new connection will be retrieved from the connection pool.
    /// </summary>
    /// <param name="callback">The callback function that is provided by the user that will be executed</param>
    /// <param name="operationName">Optional name of the operation that is executed, that will be used for observability</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="DbPolicyExecutionException">Thrown when executing the callback function failed</exception>
    /// <returns>Database result mapped to the provided type</returns>
    public async Task<TDbResult> ExecuteAsync<TDbResult>(
        StoragePolicyCallback<TDbConnection, TDbResult> callback,
        string? operationName = null,
        CancellationToken? cancellationToken = null
    )
    {
        var timer = Stopwatch.StartNew();
        var caller = this.CalledFrom();
        var releaseConnection = false;
        var success = false;

        if (ActiveConnectionContext.TryGetValue(out var connection))
        {
            Observer?.LogDebug(caller, "Retrieved active connection from async flow context");
        }
        else
        {
            // No active connection in scope so retrieve a new connection from the pool
            try
            {
                connection = await DbConnectionPool.ConnectAsync(cancellationToken);
                releaseConnection = true;
                Observer?.LogDebug(caller, "Retrieved new connection from the pool");
            }
            catch (Exception exception)
            {
                throw new DbConnectionAcquisitionException(exception);
            }
        }

        // Execute the callback function and pass the connection
        try
        {
            var result = await ExecuteCallbackAsync(callback, connection!, cancellationToken);

            Observer?.LogDebug(caller, "Finished policy execution");
            success = true;

            return result;
        }
        finally
        {
            var parentCaller = this.ParentCaller();
            Observer?.ExecutionRecordDuration(caller, timer.Elapsed, success
                ? Tags.Success.WithTag("operation_name", operationName ?? parentCaller.ToMetricPrefix())
                : Tags.Failure.WithTag("operation_name", operationName ?? parentCaller.ToMetricPrefix()));

            await CleanupAsync(releaseConnection, connection!, cancellationToken);

            Observer?.LogDebug(caller, "Connection released to the pool");
        }
    }

    /// <summary>Internal execute callback that can be overridden to execute the callback function in a different way</summary>
    /// <param name="callback">The callback function that is provided by the user that will be executed</param>
    /// <param name="connection">The active database connection that can be used to execute a database operation</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Database result mapped to the provided type</returns>
    protected abstract Task<TDbResult> ExecuteCallbackAsync<TDbResult>(
        StoragePolicyCallback<TDbConnection, TDbResult> callback,
        StorageConnectionEnvelopeBase<TDbConnection> connection,
        CancellationToken? cancellationToken = null
    );

    /// <summary>
    ///     Cleanup the connection
    /// </summary>
    /// <exception cref="DbPolicyExecutionException">Throws when unable to release the connection back to the pool</exception>
    private async Task CleanupAsync(bool releaseConnection, StorageConnectionEnvelopeBase<TDbConnection> storage,
                                    CancellationToken? cancellationToken = null)
    {
        if (!releaseConnection)
        {
            return;
        }

        try
        {
            await DbConnectionPool.ReleaseAsync(storage, cancellationToken);
        }
        catch (Exception exception)
        {
            throw new DbConnectionReleaseException(exception);
        }
    }
}
