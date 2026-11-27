using Anarchy.Toolkit.Core.Events.Local;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Core.Telemetry.Extensions.System;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.LocalEvent.Service.Storage.UnitOfWork;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

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
/// <param name="telemetry">Optional application telemetry used for logging and metrics.</param>
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
///         <item><see cref="RunInJoinedSessionAsync{TResult}" />: participates in the caller's transaction without owning it.</item>
///         <item><see cref="RunInJoinedOrManagedSessionAsync{TResult}" />: joins the caller's transaction, or owns a managed one when there is none.</item>
///     </list>
///     <para>
///     Sessions cannot be nested within the same async flow or run concurrently on one instance; register the unit
///     of work with a scoped (or transient) lifetime. <see cref="BeginAsync" />, <see cref="CommitAsync" /> and
///     <see cref="RollbackAsync" /> only act on the session this instance started in the current async flow, and a
///     session can start only one transaction. Code that joined the session cannot begin, commit or roll back.
///     </para>
///     <para>
///     A transaction is rolled back only when it exists and is still pending: a failed start leaves nothing to roll
///     back, and a transaction already committed or rolled back is not rolled back again. A rolled-back or abandoned
///     transaction publishes <see cref="RolledBackUowTransactionLocalEvent" />.
///     </para>
///     <para>
///     Cancellation is observed while acquiring the connection, beginning the transaction, running the callback and
///     just before an automatic commit; it propagates as an <see cref="OperationCanceledException" /> after rollback
///     and cleanup, which use an uncancelled token. A commit is never interrupted once started. Lifecycle events are
///     published with an uncancelled token, since they report something that already happened.
///     </para>
///     <para>
///     If rollback or cleanup also fails, the original failure is retained. A rollback failure is available in the
///     original exception's <see cref="Exception.Data"/> under <c>UnitOfWork.RollbackFailure</c>; a cleanup failure is
///     on the propagated exception under <c>UnitOfWork.CleanupFailure</c>. After a failed begin, commit or rollback,
///     or a failed transaction disposal, the connection is discarded instead of returned to the pool.
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
    AppTelemetry? telemetry = null,
    ILocalEventBus? localEventBus = null)
    : IUnitOfWork
    where TDbConnection : DbConnection where TDbTransaction : DbTransaction
{
    // The session joined by the current async flow, shared by every instance so that a participant cannot reach the
    // owner's transaction through another unit of work either.
    private static readonly AsyncLocal<StorageConnectionEnvelopeBase<TDbConnection>?> JoinedSession = new();

    private int _sessionActive;
    private bool _discardConnection;
    private StorageConnectionEnvelopeBase<TDbConnection>? _session;

    /// <summary>The transaction of the active session, or <see langword="null" /> when none is active.</summary>
    protected TDbTransaction? Transaction;

    /// <summary><c>IAsyncFlowContext</c> that contains active database connections within a unit of work session</summary>
    protected readonly IAsyncFlowContext<StorageConnectionEnvelopeBase<TDbConnection>> ActiveConnectionStorage =
        asyncFlowContext;

    /// <summary>Connection pool of the database</summary>
    protected readonly IDbConnectionPool<TDbConnection> DbConnectionPool = dbConnectionPool;

    /// <summary>Application telemetry used for logging and metrics.</summary>
    protected readonly AppTelemetry? Telemetry = telemetry;

    /// <summary>Optional in-process event bus for transaction lifecycle events.</summary>
    protected readonly ILocalEventBus? LocalEventBus = localEventBus;

    /// <summary>
    ///     Begin a new transaction on the active connection
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the transaction has started.</returns>
    /// <exception cref="NoActiveSessionException">Thrown when this instance has no session in the current async flow.</exception>
    /// <exception cref="TransactionNotOwnedException">Thrown when called from code that joined the session.</exception>
    /// <exception cref="TransactionAlreadyActiveException">Thrown when the session has already started a transaction.</exception>
    /// <exception cref="TransactionBeginException">Thrown when the transaction cannot be started.</exception>
    /// <remarks>Must be called within a session, as it uses the session connection.</remarks>
    public virtual async Task BeginAsync(CancellationToken? cancellationToken = null)
    {
        var session = GetOwnedSession() ?? throw new NoActiveSessionException();

        if (Transaction is not null)
        {
            throw new TransactionAlreadyActiveException();
        }

        try
        {
            Transaction = (TDbTransaction)await session.DataStorageConnection.BeginTransactionAsync(
                cancellationToken ?? CancellationToken.None);
        }
        catch (Exception exception)
        {
            // The connection state is unknown after a failed start
            _discardConnection = true;

            if (exception is OperationCanceledException && cancellationToken?.IsCancellationRequested == true)
            {
                throw;
            }

            throw new TransactionBeginException(exception);
        }

        session.IsInTransaction = true;

        await PublishAsync(StartedUowTransactionLocalEvent.FromConnectionEnvelope(session, GetType().Name));
    }

    /// <summary>
    ///     Commit the transaction containing the database changes
    /// </summary>
    /// <param name="suppressNoActiveTransactionException">
    ///     When <c>true</c>, does not throw if there is no active transaction to commit.
    ///     This is useful for defensive commit calls in callback-based flows.
    /// </param>
    /// <param name="cancellationToken">
    ///     Token checked before the commit starts. The commit itself is not interrupted, since an interrupted commit
    ///     leaves its outcome unknown.
    /// </param>
    /// <returns>A task that completes when the transaction has been committed.</returns>
    /// <exception cref="NoActiveTransactionException">
    ///     Thrown when there is no active transaction and <paramref name="suppressNoActiveTransactionException" /> is <c>false</c>.
    /// </exception>
    /// <exception cref="TransactionNotOwnedException">Thrown when called from code that joined the session.</exception>
    /// <exception cref="TransactionCommitException">Thrown when the commit fails; the connection is then discarded.</exception>
    public virtual async Task CommitAsync(bool suppressNoActiveTransactionException = false,
                                          CancellationToken? cancellationToken = null)
    {
        var session = GetOwnedSession();
        if (session is null && _session is not null)
        {
            throw new NoActiveTransactionException();
        }

        if (Transaction is null || session is not { IsInTransaction: true })
        {
            if (suppressNoActiveTransactionException)
            {
                return;
            }

            throw new NoActiveTransactionException();
        }

        cancellationToken?.ThrowIfCancellationRequested();

        try
        {
            await Transaction.CommitAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            // The transaction stays pending so that the session can still roll it back
            _discardConnection = true;
            throw new TransactionCommitException(exception);
        }

        session.IsInTransaction = false;

        await PublishAsync(CommittedUowTransactionLocalEvent.FromConnectionEnvelope(session, GetType().Name));
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
    /// <exception cref="TransactionNotOwnedException">Thrown when called from code that joined the session.</exception>
    /// <exception cref="TransactionRollbackException">Thrown when the rollback fails.</exception>
    /// <remarks>
    ///     If the rollback fails or is cancelled, the transaction is abandoned: it is no longer pending, the connection
    ///     is discarded (which aborts it on the server) and <see cref="RolledBackUowTransactionLocalEvent" /> is still
    ///     published.
    /// </remarks>
    public virtual async Task RollbackAsync(Exception? reason = null,
                                            bool suppressNoActiveTransactionException = false,
                                            CancellationToken? cancellationToken = null)
    {
        var session = GetOwnedSession();
        if (session is null && _session is not null)
        {
            throw new NoActiveTransactionException();
        }

        if (Transaction is null || session is not { IsInTransaction: true })
        {
            if (suppressNoActiveTransactionException)
            {
                return;
            }

            throw new NoActiveTransactionException();
        }

        Exception? failure = null;
        try
        {
            await Transaction.RollbackAsync(cancellationToken ?? CancellationToken.None);
        }
        catch (Exception exception)
        {
            _discardConnection = true;
            failure = exception;
        }

        session.IsInTransaction = false;

        try
        {
            await PublishAsync(RolledBackUowTransactionLocalEvent.FromConnectionEnvelope(session, GetType().Name, reason));
        }
        catch (Exception notificationFailure) when (failure is not null)
        {
            failure.Data["UnitOfWork.RollbackNotificationFailure"] = notificationFailure;
        }

        if (failure is OperationCanceledException && cancellationToken?.IsCancellationRequested == true)
        {
            ExceptionDispatchInfo.Throw(failure);
        }

        if (failure is not null)
        {
            throw new TransactionRollbackException(failure);
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
        ArgumentNullException.ThrowIfNull(callback);

        await RunInManagedSessionAsync(async ct => {
            await callback(ct);
            return true;
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
    ///     Run a callback as a participant in the transaction of the session active in the current async flow, which
    ///     the caller owns.
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the caller's transaction</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="TResult">Type of the callback result.</typeparam>
    /// <exception cref="NoActiveTransactionException">
    ///     Thrown when no session with a pending transaction is active in the current async flow.
    /// </exception>
    /// <returns>The result returned by <paramref name="callback" />.</returns>
    /// <remarks>
    ///     The callback cannot begin, commit or roll back, through this or any other unit of work
    ///     (<see cref="TransactionNotOwnedException" />). Its exceptions propagate unchanged, so the owner decides
    ///     whether to roll back.
    /// </remarks>
    public async Task<TResult> RunInJoinedSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                                CancellationToken? cancellationToken = null)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (!ActiveConnectionStorage.TryGetValue(out var session) || session is not { IsInTransaction: true })
        {
            throw new NoActiveTransactionException();
        }

        var previous = JoinedSession.Value;
        JoinedSession.Value = session;
        try
        {
            return await callback(cancellationToken);
        }
        finally
        {
            JoinedSession.Value = previous;
        }
    }

    /// <summary>
    ///     Join the caller's transaction when one is pending in the current async flow
    ///     (<see cref="RunInJoinedSessionAsync{TResult}" />); otherwise start and own a managed session
    ///     (<see cref="RunInManagedSessionAsync{TResult}" />).
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the transaction</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="TResult">Type of the callback result.</typeparam>
    /// <exception cref="SessionAlreadyActiveException">Thrown when the caller's session has no pending transaction.</exception>
    /// <returns>The result returned by <paramref name="callback" />.</returns>
    public async Task<TResult> RunInJoinedOrManagedSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                                         CancellationToken? cancellationToken = null)
    {
        if (ActiveConnectionStorage.TryGetValue(out var session) && session is { IsInTransaction: true })
        {
            return await RunInJoinedSessionAsync(callback, cancellationToken);
        }

        return await RunInManagedSessionAsync(callback, cancellationToken);
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
    /// <remarks>
    ///     A transaction still pending when the session ends, after a failure or because the callback of
    ///     <see cref="RunInSessionAsync{TResult}" /> neither committed nor rolled back, is rolled back.
    /// </remarks>
    protected async Task<TResult> StartAsync<TResult>(SessionCallback<TResult> callback,
                                                      bool autoManage,
                                                      bool enableTransaction,
                                                      CancellationToken? cancellationToken = null
    )
    {
        ArgumentNullException.ThrowIfNull(callback);

        var timer = Stopwatch.StartNew();
        var caller = this.CalledFrom();

        // Make sure we are not starting a new session within an active session
        if (ActiveConnectionStorage.HasValue || Interlocked.CompareExchange(ref _sessionActive, 1, 0) != 0)
        {
            throw new SessionAlreadyActiveException();
        }

        try
        {
            // Retrieve a new connection for the session
            var session = await DbConnectionPool.ConnectAsync(cancellationToken);
            _session = session;

            // All the DbPolicies can now use this connection when retrieving
            // it within the same scope of the callback.
            return await ActiveConnectionStorage.RunAsync(session, async Task<TResult> () => {
                Exception? sessionFailure = null;
                try
                {
                    // Begin a new transaction
                    if (enableTransaction)
                    {
                        await BeginAsync(cancellationToken);
                        Telemetry?.Log(LogLevel.Debug, caller, null, "Transaction started");
                    }

                    Telemetry?.Log(LogLevel.Information, caller, null, "Session started");

                    var sessionResult = await callback(cancellationToken);

                    // Auto commit
                    if (autoManage && enableTransaction)
                    {
                        await CommitAsync(cancellationToken: cancellationToken);
                        Telemetry?.Log(LogLevel.Debug, caller, null, "Transaction committed");
                    }
                    else if (HasPendingTransaction(session))
                    {
                        await RollbackAsync(cancellationToken: CancellationToken.None);
                        Telemetry?.Log(LogLevel.Debug, caller, null, "Rolled back the transaction left pending by the session");
                    }

                    Telemetry?.Log(LogLevel.Information, caller, null, "Session finished successfully");
                    Telemetry?.RecordDuration(caller, timer.Elapsed, [new("success", true)]);

                    return sessionResult;
                }
                catch (Exception ex)
                {
                    // Roll back only a transaction that was started and is still pending
                    if (HasPendingTransaction(session))
                    {
                        try
                        {
                            await RollbackAsync(ex, cancellationToken: CancellationToken.None);
                            Telemetry?.Log(LogLevel.Debug, caller, null, "Rolled back transaction");
                        }
                        catch (Exception rollbackFailure)
                        {
                            ex.Data["UnitOfWork.RollbackFailure"] = rollbackFailure;
                        }
                    }

                    Telemetry?.Log(LogLevel.Error, caller, ex, "Failed to execute session callback");
                    Telemetry?.RecordDuration(caller, timer.Elapsed, [new("success", false)]);

                    // Re-throw the error
                    if (ex is OperationCanceledException && cancellationToken?.IsCancellationRequested == true)
                    {
                        sessionFailure = ex;
                        throw;
                    }

                    sessionFailure = new SessionCallbackException(ex);
                    throw sessionFailure;
                }
                finally
                {
                    try
                    {
                        await FinishSessionAsync(CancellationToken.None);
                        Telemetry?.Log(LogLevel.Debug, caller, null, "Released connection back to the pool");
                    }
                    catch (Exception cleanupFailure) when (sessionFailure is not null)
                    {
                        sessionFailure.Data["UnitOfWork.CleanupFailure"] = cleanupFailure;
                    }
                }
            }, cancellationToken);
        }
        finally
        {
            _session = null;
            Transaction = null;
            _discardConnection = false;
            Volatile.Write(ref _sessionActive, 0);
        }
    }

    /// <summary>
    ///     Finish the active session and release the connection back to the pool.
    ///     Any transaction still pending (neither committed nor rolled back) is rolled back by disposing it,
    ///     so the connection never returns to the pool with an open transaction.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the session has been finished.</returns>
    /// <exception cref="SessionFinishException">Throws when unable to release the connection back to the pool</exception>
    /// <remarks>
    ///     Idempotent: the transaction and the connection are disposed or released at most once per session, and
    ///     later calls do nothing.
    /// </remarks>
    protected async Task FinishSessionAsync(CancellationToken? cancellationToken = null)
    {
        var session = _session;
        if (session is null)
        {
            return;
        }

        _session = null;
        session.IsInTransaction = false;

        Exception? failure = null;
        if (Transaction is not null)
        {
            var transaction = Transaction;
            Transaction = null;
            try
            {
                await transaction.DisposeAsync();
            }
            catch (Exception exception)
            {
                _discardConnection = true;
                failure = exception;
            }
        }

        try
        {
            if (_discardConnection)
            {
                await session.DataStorageConnection.DisposeAsync();
            }
            else
            {
                await DbConnectionPool.ReleaseAsync(session, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            if (failure is null) failure = exception;
            else failure.Data["UnitOfWork.ConnectionCleanupFailure"] = exception;
        }

        try
        {
            if (ActiveConnectionStorage.TryGetValue(out var current) && ReferenceEquals(current, session))
            {
                ActiveConnectionStorage.Clear();
            }
        }
        catch (Exception exception)
        {
            if (failure is null) failure = exception;
            else failure.Data["UnitOfWork.ContextCleanupFailure"] = exception;
        }

        if (failure is not null) throw new SessionFinishException(failure);
    }

    /// <summary>
    ///     Gets the session this instance started, when it is the one active in the current async flow.
    /// </summary>
    /// <exception cref="TransactionNotOwnedException">Thrown when the current async flow joined the active session.</exception>
    private StorageConnectionEnvelopeBase<TDbConnection>? GetOwnedSession()
    {
        if (!ActiveConnectionStorage.TryGetValue(out var current) || current is null)
        {
            return null;
        }

        if (ReferenceEquals(JoinedSession.Value, current))
        {
            throw new TransactionNotOwnedException();
        }

        return ReferenceEquals(current, _session) ? current : null;
    }

    private bool HasPendingTransaction(StorageConnectionEnvelopeBase<TDbConnection> session) =>
        Transaction is not null && ReferenceEquals(session, _session) && session.IsInTransaction;

    private async Task PublishAsync<TEvent>(TEvent @event) where TEvent : ILocalEvent
    {
        if (LocalEventBus is not null)
        {
            await LocalEventBus.PublishAsync(@event, ct: CancellationToken.None);
        }
    }
}
