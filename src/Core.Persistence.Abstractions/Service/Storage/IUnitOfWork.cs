namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;

/// <summary>
///     Callback function type that can be used in policies to handle operations in a function provided by the user
/// </summary>
public delegate Task<TResult> SessionCallback<TResult>(CancellationToken? cancellationToken = null);

/// <summary>
///     Callback function type that can be used in policies to handle operations in a function provided by the user
/// </summary>
public delegate Task SessionCallback(CancellationToken? cancellationToken = null);

/// <summary>
///     Interface of a unit of work pattern.
///     A unit of work contains can start a session that will create a connection and transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    ///     Start a new session without starting a transaction scope. The connection scope is still managed by the session
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<TResult> RunInAdhocSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                  CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Start a new session that will manage the transaction scope of the session automatically
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<TResult> RunInSessionAsync<TResult>(SessionCallback<TResult> callback,
                                             CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Start a new session that will manage the transaction scope of the session automatically
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<TResult> RunInManagedSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                    CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Start a new session that will manage the transaction scope of the session automatically
    /// </summary>
    /// <param name="callback">The user provided callback function that will be executed within the scope of the session</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task RunInManagedSessionAsync(SessionCallback callback, CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Begin a new transaction within the active session
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task BeginAsync(CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Commit the transaction within the session
    /// </summary>
    /// <param name="suppressNoActiveTransactionException">
    ///     When <c>true</c>, does not throw if there is no active transaction to commit.
    ///     Use this for callback-driven flows where commit may be invoked defensively.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task CommitAsync(bool suppressNoActiveTransactionException = false,
                     CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Rollback the transaction within the session
    /// </summary>
    /// <param name="reason">Exception that made the rollback necessary (optional)</param>
    /// <param name="suppressNoActiveTransactionException">
    ///     When <c>true</c>, does not throw if there is no active transaction to roll back.
    ///     Use this for callback-driven flows where rollback may be invoked defensively.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task RollbackAsync(Exception? reason = null,
                       bool suppressNoActiveTransactionException = false,
                       CancellationToken? cancellationToken = null);
}
