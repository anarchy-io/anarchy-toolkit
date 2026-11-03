using Anarchy.Toolkit.Core.Events.Service.Event.InMemory;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.LocalEvent.Service.Storage.UnitOfWork;
using System.Data.Common;
using System.Diagnostics;
using System.Transactions;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Abstract implementation of a unit of work pattern.
///     A unit of work contains can start a session that will create a connection and transaction.
///     Within the lifecycle of the session database actions can be executed.
///     When the session ends it will finish the transaction and return the connection back to the pool.
/// </summary>
public abstract class DbUnitOfWorkBase<TDbConnection, TDbTransaction>(
    IDbConnectionPool<TDbConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<TDbConnection>> asyncFlowContext,
    Observer? observer = null,
    ILocalEventBus? localEventBus = null)
    : IUnitOfWork
    where TDbConnection : DbConnection where TDbTransaction : DbTransaction
{
    protected TDbTransaction? Transaction;

    /// <summary><c>IAsyncFlowContext</c> that contains active database connections within a unit of work session</summary>
    protected readonly IAsyncFlowContext<StorageConnectionEnvelopeBase<TDbConnection>> ActiveConnectionStorage =
        asyncFlowContext;

    /// <summary>Connection pool of the database</summary>
    protected readonly IDbConnectionPool<TDbConnection> DbConnectionPool = dbConnectionPool;

    /// <summary>Observer used for logging and metrics</summary>
    protected readonly Observer? Observer = observer;

    /// <summary>Optional in-process event bus for transaction lifecycle events.</summary>
    protected readonly ILocalEventBus? LocalEventBus = localEventBus;

    /// <summary>
    ///     Begin a new transaction on the active connection
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public virtual async Task BeginAsync(CancellationToken? cancellationToken = null)
    {
        var connection = ActiveConnectionStorage.Value.DataStorageConnection;

        try
        {
            Transaction = (TDbTransaction)await connection.BeginTransactionAsync(
                cancellationToken ?? CancellationToken.None);

            if (LocalEventBus is not null)
            {
                await LocalEventBus.PublishAsync(
                    StartedUowTransactionLocalEvent.FromConnectionEnvelope(ActiveConnectionStorage.Value,
                        GetType().Name),
                    ct: cancellationToken ?? CancellationToken.None);
            }

            ActiveConnectionStorage.Value.IsInTransaction = true;
        }
        catch (Exception exception)
        {
            throw new TransactionBeginException(exception);
        }
    }

    /// <summary>
    ///     Commit the transaction containing the database changes
    /// </summary>
    /// <param name="suppressNoActiveTransactionException">
    ///     When <c>true</c>, does not throw if there is no active transaction to commit.
    ///     This is useful for defensive commit calls in callback-based flows.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public virtual async Task CommitAsync(bool suppressNoActiveTransactionException = false,
                                          CancellationToken? cancellationToken = null)
    {
        if (Transaction == null)
        {
            if (suppressNoActiveTransactionException)
            {
                return;
            }

            throw new NoActiveTransactionException();
        }

        try
        {
            await Transaction.CommitAsync(cancellationToken ?? CancellationToken.None);

            if (LocalEventBus is not null)
            {
                await LocalEventBus.PublishAsync(
                    CommittedUowTransactionLocalEvent.FromConnectionEnvelope(ActiveConnectionStorage.Value,
                        GetType().Name),
                    ct: cancellationToken ?? CancellationToken.None);
            }
            ActiveConnectionStorage.Value.IsInTransaction = false;
        }
        catch (Exception exception)
        {
            throw new TransactionCommitException(exception);
        }
    }

    /// <summary>
    ///     Rollback the transaction containing the database changes
    /// </summary>
    /// <param name="reason">Exception that made the rollback necessary (optional)</param>
    /// <param name="suppressNoActiveTransactionException">
    ///     When <c>true</c>, does not throw if there is no active transaction to roll back.
    ///     This is useful for defensive rollback calls in callback-based flows.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public virtual async Task RollbackAsync(Exception? reason = null,
                                            bool suppressNoActiveTransactionException = false,
                                            CancellationToken? cancellationToken = null)
    {
        if (Transaction == null)
        {
            if (suppressNoActiveTransactionException)
            {
                return;
            }

            throw new NoActiveTransactionException();
        }

        try
        {
            await Transaction.RollbackAsync(cancellationToken ?? CancellationToken.None);

            if (LocalEventBus is not null)
            {
                await LocalEventBus
                        .PublishAsync(
                            RolledBackUowTransactionLocalEvent.FromConnectionEnvelope(ActiveConnectionStorage.Value,
                                GetType().Name, reason),
                            ct: cancellationToken ?? CancellationToken.None);
            }
            ActiveConnectionStorage.Value.IsInTransaction = false;
        }
        catch (Exception exception)
        {
            throw new TransactionRollbackException(exception);
        }
    }

    /// <summary>
    ///     Start a new session where no transaction scope is started.
    ///     This can be used for read only operations or write operation where not transaction is required.
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="TransactionException">Throws when unable stop the session</exception>
    /// <returns>Optional result from the session</returns>
    public async Task<TResult> RunInAdhocSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                               CancellationToken? cancellationToken = null)
    {
        return await StartAsync(callback, true, false, cancellationToken);
    }

    /// <summary>
    ///     Start a new session that will manage the transaction scope of the session automatically.
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="TransactionException">Throws when unable to start the session</exception>
    /// <returns>Optional result from the session</returns>
    public async Task<TResult> RunInManagedSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                                 CancellationToken? cancellationToken = null)
    {
        return await StartAsync(callback, true, true, cancellationToken);
    }

    public async Task RunInManagedSessionAsync(SessionCallback callback, CancellationToken? cancellationToken = null)
    {
        await RunInManagedSessionAsync(async ct => {
            await callback(ct);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    /// <summary>
    ///     Start a new session.
    ///     To persist any change in the database make sure to call commit() within the provided callback.
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="TransactionException">Throws when unable to start the session</exception>
    /// <returns>Optional result from the session</returns>
    public async Task<TResult> RunInSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                          CancellationToken? cancellationToken = null)
    {
        return await StartAsync(callback, false, true, cancellationToken);
    }

    /// <summary>
    ///     Internal method the Start a new session
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="autoManage">Whether to manage the transaction scope of the session automatically</param>
    /// <param name="enableTransaction">Whether the session should start a transaction</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="TransactionException">Throws when unable to start the session</exception>
    /// <exception cref="UnitOfWorkSessionException">Throws when unable to execute the callback</exception>
    /// <returns>Optional result from the session</returns>
    protected async Task<TResult> StartAsync<TResult>(SessionCallback<TResult> callback,
                                                      bool autoManage,
                                                       bool enableTransaction,
                                                       CancellationToken? cancellationToken = null
    )
    {
        var timer = Stopwatch.StartNew();
        var caller = this.CalledFrom();

        // Make sure we are not starting a new session within an active session
        if (ActiveConnectionStorage.HasValue)
        {
            throw new SessionAlreadyActiveException();
        }

        // Retrieve a new connection for the session
        var connectionEnvelope = await DbConnectionPool.ConnectAsync(cancellationToken);

        // All the DbPolicies can now use this connection when retrieving
        // it within the same scope of the callback.
        return await ActiveConnectionStorage.RunAsync(connectionEnvelope, async Task<TResult> () => {
            try
            {
                // Begin a new transaction
                if (enableTransaction)
                {
                    await BeginAsync(cancellationToken);
                    Observer?.LogDebug(caller, "Transaction started");
                }

                Observer?.LogInformation(caller, "Session started");

                var sessionResult = await callback(cancellationToken);

                // Auto commit
                if (autoManage && enableTransaction)
                {
                    await CommitAsync(cancellationToken: cancellationToken);
                    Observer?.LogDebug(caller, "Transaction committed");
                }

                Observer?.LogInformation(caller, "Session finished successfully");
                Observer?.ExecutionRecordDuration(caller, timer.Elapsed, Tags.Success);

                return sessionResult;
            }
            catch (Exception ex)
            {
                // Auto rollback
                if (autoManage && enableTransaction)
                {
                    await RollbackAsync(ex, cancellationToken: cancellationToken);
                    Observer?.LogDebug(caller, "Rolled back transaction");
                }

                Observer?.LogError(ex, caller, "Failed to execute session callback");
                Observer?.ExecutionRecordDuration(caller, timer.Elapsed, Tags.Failure);

                // Re-throw the error
                throw new SessionCallbackException(ex);
            }
            finally
            {
                await FinishSessionAsync(cancellationToken);
                Observer?.LogDebug(caller, "Released connection back to the pool");
            }
        }, cancellationToken);
    }

    /// <summary>
    ///     Finish the active session and release the connection back to the pool.
    ///     Any transaction still pending (neither committed nor rolled back) is rolled back by disposing it,
    ///     so the connection never returns to the pool with an open transaction.
    /// </summary>
    /// <exception cref="TransactionException">Throws when unable to release the connection back to the pool</exception>
    protected async Task FinishSessionAsync(CancellationToken? cancellationToken = null)
    {
        try
        {
            try
            {
                if (Transaction is not null)
                {
                    var transaction = Transaction;
                    Transaction = null;
                    await transaction.DisposeAsync();
                }
            }
            finally
            {
                await DbConnectionPool.ReleaseAsync(ActiveConnectionStorage.Value, cancellationToken);
            }

            ActiveConnectionStorage.Clear();
        }
        catch (Exception ex)
        {
            throw new SessionFinishException(ex);
        }
    }
}
