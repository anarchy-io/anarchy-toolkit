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
///     A unit of work can start a session that will create a connection and transaction.
///     Within the lifecycle of the session database actions can be executed.
///     When the session ends it will finish the transaction and return the connection back to the pool.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <typeparam name="TDbTransaction">The provider transaction type returned by <see cref="DbConnection.BeginTransactionAsync(System.Threading.CancellationToken)" />.</typeparam>
/// <param name="dbConnectionPool">Connection pool the session connection is taken from and returned to.</param>
/// <param name="asyncFlowContext">
///     Async flow context in which the session connection is stored; it must be the same instance used by the
///     <see cref="DbPolicyBase{TDbConnection}" /> of the repositories so they join the session.
/// </param>
/// <param name="observer">Optional observer used for logging and metrics.</param>
/// <param name="localEventBus">
///     Optional bus on which <see cref="StartedUowTransactionLocalEvent" />, <see cref="CommittedUowTransactionLocalEvent" />
///     and <see cref="RolledBackUowTransactionLocalEvent" /> are published.
/// </param>
/// <remarks>
///     <para>Session flavours:</para>
///     <list type="bullet">
///         <item><see cref="RunInManagedSessionAsync{TResult}" />: transaction committed on success, rolled back on failure.</item>
///         <item><see cref="RunInSessionAsync{TResult}" />: transaction started, but the callback must call <see cref="CommitAsync" /> / <see cref="RollbackAsync" />; anything left pending is rolled back when the session ends.</item>
///         <item><see cref="RunInAdhocSessionAsync{TResult}" />: shared connection without a transaction.</item>
///     </list>
///     <para>
///     Sessions cannot be nested within the same async flow. The transaction is stored on the instance, so a single
///     instance must not run concurrent sessions; register it with a scoped (or transient) lifetime.
///     </para>
/// </remarks>
/// <example>
///     <code language="csharp">
///     var orderId = await unitOfWork.RunInManagedSessionAsync(async ct =>
///     {
///         var order = await orderWriterRepository.SaveAsync(newOrder, ct);
///         await taskOutboxWriterRepository.SaveAllAsync([orderCreatedEvent], ct);
///         return order.Id;
///     }, cancellationToken);
///     </code>
/// </example>
public abstract class DbUnitOfWorkBase<TDbConnection, TDbTransaction>(
    IDbConnectionPool<TDbConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<TDbConnection>> asyncFlowContext,
    Observer? observer = null,
    ILocalEventBus? localEventBus = null)
    : IUnitOfWork
    where TDbConnection : DbConnection where TDbTransaction : DbTransaction
{
    /// <summary>The transaction of the active session, or <see langword="null" /> when none is active.</summary>
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
    /// <returns>A task that completes when the transaction has started.</returns>
    /// <exception cref="TransactionBeginException">Thrown when the transaction cannot be started.</exception>
    /// <remarks>Must be called within a session, as it uses the session connection.</remarks>
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
    /// <returns>A task that completes when the transaction has been committed.</returns>
    /// <exception cref="NoActiveTransactionException">
    ///     Thrown when there is no active transaction and <paramref name="suppressNoActiveTransactionException" /> is <c>false</c>.
    /// </exception>
    /// <exception cref="TransactionCommitException">Thrown when the commit fails.</exception>
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
    /// <returns>A task that completes when the transaction has been rolled back.</returns>
    /// <exception cref="NoActiveTransactionException">
    ///     Thrown when there is no active transaction and <paramref name="suppressNoActiveTransactionException" /> is <c>false</c>.
    /// </exception>
    /// <exception cref="TransactionRollbackException">Thrown when the rollback fails.</exception>
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
    /// <typeparam name="TResult">Type of the session result.</typeparam>
    /// <exception cref="SessionAlreadyActiveException">Thrown when a session is already active in the current async flow.</exception>
    /// <exception cref="SessionCallbackException">Thrown, wrapping the original exception, when the callback (or the automatic commit) fails.</exception>
    /// <exception cref="SessionFinishException">Thrown when the connection cannot be released at the end of the session.</exception>
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
    /// <typeparam name="TResult">Type of the session result.</typeparam>
    /// <exception cref="SessionAlreadyActiveException">Thrown when a session is already active in the current async flow.</exception>
    /// <exception cref="SessionCallbackException">Thrown, wrapping the original exception, when the callback (or the automatic commit) fails.</exception>
    /// <exception cref="SessionFinishException">Thrown when the connection cannot be released at the end of the session.</exception>
    /// <returns>Optional result from the session</returns>
    public async Task<TResult> RunInManagedSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                                 CancellationToken? cancellationToken = null)
    {
        return await StartAsync(callback, true, true, cancellationToken);
    }

    /// <summary>
    ///     Start a new session that will manage the transaction scope of the session automatically, for callbacks
    ///     without a result.
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="SessionAlreadyActiveException">Thrown when a session is already active in the current async flow.</exception>
    /// <exception cref="SessionCallbackException">Thrown, wrapping the original exception, when the callback (or the automatic commit) fails.</exception>
    /// <exception cref="SessionFinishException">Thrown when the connection cannot be released at the end of the session.</exception>
    /// <returns>A task that completes when the session has finished.</returns>
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
    /// <typeparam name="TResult">Type of the session result.</typeparam>
    /// <exception cref="SessionAlreadyActiveException">Thrown when a session is already active in the current async flow.</exception>
    /// <exception cref="SessionCallbackException">Thrown, wrapping the original exception, when the callback (or the automatic commit) fails.</exception>
    /// <exception cref="SessionFinishException">Thrown when the connection cannot be released at the end of the session.</exception>
    /// <returns>Optional result from the session</returns>
    /// <remarks>Any transaction neither committed nor rolled back by the callback is rolled back when the session ends.</remarks>
    public async Task<TResult> RunInSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                          CancellationToken? cancellationToken = null)
    {
        return await StartAsync(callback, false, true, cancellationToken);
    }

    /// <summary>
    ///     Starts a new session: acquires a connection, stores it in the async flow context for the duration of
    ///     <paramref name="callback" />, optionally begins (and auto-commits / auto-rolls back) a transaction, and
    ///     finally releases the connection.
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="autoManage">Whether to manage the transaction scope of the session automatically</param>
    /// <param name="enableTransaction">Whether the session should start a transaction</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="TResult">Type of the session result.</typeparam>
    /// <exception cref="SessionAlreadyActiveException">Thrown when a session is already active in the current async flow.</exception>
    /// <exception cref="SessionCallbackException">Thrown, wrapping the original exception, when the callback (or the automatic commit) fails.</exception>
    /// <exception cref="SessionFinishException">Thrown when the connection cannot be released at the end of the session.</exception>
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
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the session has been finished.</returns>
    /// <exception cref="SessionFinishException">Throws when unable to release the connection back to the pool</exception>
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
