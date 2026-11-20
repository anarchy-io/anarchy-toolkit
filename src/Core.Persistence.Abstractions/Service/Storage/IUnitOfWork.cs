namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;

/// <summary>
///     Callback function type that can be used in policies to handle operations in a function provided by the user
/// </summary>
/// <remarks>Executed by <see cref="IUnitOfWork"/> within the scope of a session.</remarks>
/// <typeparam name="TResult">Type of the session result.</typeparam>
/// <param name="cancellationToken">Token used to cancel the operation.</param>
/// <returns>The session result.</returns>
public delegate Task<TResult> SessionCallback<TResult>(CancellationToken? cancellationToken = null);

/// <summary>
///     Callback function type that can be used in policies to handle operations in a function provided by the user
/// </summary>
/// <remarks>Executed by <see cref="IUnitOfWork"/> within the scope of a session; variant without a result.</remarks>
/// <param name="cancellationToken">Token used to cancel the operation.</param>
/// <returns>A task that completes when the callback has finished.</returns>
public delegate Task SessionCallback(CancellationToken? cancellationToken = null);

/// <summary>
///     Interface of a unit-of-work pattern. A unit of work can start a session that creates a connection and transaction.
/// </summary>
/// <remarks>
///     <para>
///     A session acquires a storage connection and makes it available, through an <see cref="IAsyncFlowContext{TValue}"/>,
///     to every <see cref="IStoragePolicy{TStorage}"/> (and therefore repository) invoked within the callback's async
///     flow. Sessions cannot be nested: starting a session while one is active in the same async flow fails with
///     <see cref="Exceptions.SessionAlreadyActiveException"/>.
///     </para>
///     <para>Choosing a session type:</para>
///     <list type="bullet">
///         <item><description><see cref="RunInAdhocSessionAsync{TResult}"/>: no transaction (read-only or non-transactional writes).</description></item>
///         <item><description><c>RunInManagedSessionAsync</c>: a transaction is started and committed automatically on success, rolled back on failure.</description></item>
///         <item><description><see cref="RunInSessionAsync{TResult}"/>: a transaction is started but the callback must call <see cref="CommitAsync"/> itself.</description></item>
///         <item><description><see cref="RunInJoinedSessionAsync{TResult}"/>: joins the transaction of the caller's session without owning it.</description></item>
///         <item><description><see cref="RunInJoinedOrManagedSessionAsync{TResult}"/>: joins the caller's transaction when there is one, otherwise owns a managed one.</description></item>
///     </list>
///     <para>
///     Transaction ownership: the code that starts a session owns its transaction and is the only one that commits or
///     rolls it back. Code that joins it (for example an aggregate called by a UseCase) cannot begin, commit or roll
///     back (<see cref="Exceptions.TransactionNotOwnedException"/>); it signals failure by throwing, and the owner
///     decides.
///     </para>
///     <para>
///     Cancellation: the token is observed while acquiring the connection, beginning the transaction, running the
///     callback and just before an automatic commit. A commit is never interrupted once started; rollbacks and cleanup
///     after a failure run with an uncancelled token. Caller-requested cancellation propagates as an
///     <see cref="OperationCanceledException"/>, after the transaction was rolled back and the connection cleaned up.
///     </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// var saved = await unitOfWork.RunInManagedSessionAsync(async ct =&gt;
/// {
///     var order = await orderWriter.SaveAsync(newOrder, ct);
///     await outboxWriter.SaveAsync(new OrderCreatedOutbox(order.Id), ct);
///     return order;
/// }, cancellationToken);
/// </code>
/// </example>
public interface IUnitOfWork
{
    /// <summary>
    ///     Start a new session without starting a transaction scope. The connection scope is still managed by the session
    /// </summary>
    /// <typeparam name="TResult">Type of the session result.</typeparam>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The result returned by <paramref name="callback"/>.</returns>
    Task<TResult> RunInAdhocSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                  CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Start a new session with a transaction. The transaction is not committed automatically: to persist any change
    ///     call <see cref="CommitAsync"/> within the provided callback.
    /// </summary>
    /// <typeparam name="TResult">Type of the session result.</typeparam>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The result returned by <paramref name="callback"/>.</returns>
    Task<TResult> RunInSessionAsync<TResult>(SessionCallback<TResult> callback,
                                             CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Start a new session that will manage the transaction scope of the session automatically
    /// </summary>
    /// <remarks>The transaction is committed when the callback succeeds and rolled back when it throws.</remarks>
    /// <typeparam name="TResult">Type of the session result.</typeparam>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The result returned by <paramref name="callback"/>.</returns>
    Task<TResult> RunInManagedSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                    CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Start a new session that will manage the transaction scope of the session automatically
    /// </summary>
    /// <remarks>
    ///     Variant of <see cref="RunInManagedSessionAsync{TResult}"/> for callbacks without a result.
    /// </remarks>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the session has finished.</returns>
    Task RunInManagedSessionAsync(SessionCallback callback, CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Run a callback as a participant in the transaction of the session active in the current async flow, which
    ///     the caller owns.
    /// </summary>
    /// <remarks>
    ///     The callback shares the caller's connection and transaction. It cannot begin, commit or roll back
    ///     (<see cref="Exceptions.TransactionNotOwnedException"/>), and its exceptions propagate unchanged so the owner
    ///     decides whether to roll back.
    /// </remarks>
    /// <typeparam name="TResult">Type of the callback result.</typeparam>
    /// <param name="callback">The user provided callback function that will be executed within the caller's transaction</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The result returned by <paramref name="callback"/>.</returns>
    /// <exception cref="Exceptions.NoActiveTransactionException">
    ///     Thrown when no session with a pending transaction is active in the current async flow.
    /// </exception>
    Task<TResult> RunInJoinedSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                   CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Join the caller's transaction when one is pending in the current async flow
    ///     (<see cref="RunInJoinedSessionAsync{TResult}"/>); otherwise start and own a managed session
    ///     (<see cref="RunInManagedSessionAsync{TResult}"/>).
    /// </summary>
    /// <remarks>
    ///     Use it for code, such as an aggregate, that must be atomic on its own but can also take part in a wider
    ///     transaction started by its caller. A caller's session without a transaction is not joined: starting a managed
    ///     session within it fails with <see cref="Exceptions.SessionAlreadyActiveException"/>.
    /// </remarks>
    /// <typeparam name="TResult">Type of the callback result.</typeparam>
    /// <param name="callback">The user provided callback function that will be executed within the transaction</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The result returned by <paramref name="callback"/>.</returns>
    Task<TResult> RunInJoinedOrManagedSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                            CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Begin a new transaction within the active session
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the transaction has started.</returns>
    Task BeginAsync(CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Commit the transaction within the session
    /// </summary>
    /// <remarks>
    ///     The token is checked before the commit starts; the commit itself is not interrupted, since an interrupted
    ///     commit leaves its outcome unknown.
    /// </remarks>
    /// <param name="suppressNoActiveTransactionException">
    ///     When <c>true</c>, does not throw if there is no active transaction to commit.
    ///     Use this for callback-driven flows where commit may be invoked defensively.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the transaction has been committed.</returns>
    Task CommitAsync(bool suppressNoActiveTransactionException = false,
                     CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Rollback the transaction within the session
    /// </summary>
    /// <remarks>
    ///     If the rollback fails or is cancelled, the transaction is abandoned: its connection is discarded instead of
    ///     returned to the pool, which aborts it on the server.
    /// </remarks>
    /// <param name="reason">Exception that made the rollback necessary (optional)</param>
    /// <param name="suppressNoActiveTransactionException">
    ///     When <c>true</c>, does not throw if there is no active transaction to roll back.
    ///     Use this for callback-driven flows where rollback may be invoked defensively.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the transaction has been rolled back.</returns>
    Task RollbackAsync(Exception? reason = null,
                       bool suppressNoActiveTransactionException = false,
                       CancellationToken? cancellationToken = null);
}
