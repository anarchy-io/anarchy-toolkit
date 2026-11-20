using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

namespace Anarchy.Toolkit.Samples.Examples.Orders.Infrastructure;

/// <summary>
/// A unit of work over in-memory stores, so the examples and tests run without a database. Writes made in a
/// transaction are applied when the owner commits and dropped when it rolls back; only the session that started the
/// transaction owns it.
/// </summary>
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly AsyncLocal<Session?> _current = new();

    /// <summary>Applies the write now, or when the transaction in the current flow commits.</summary>
    public void Enlist(Action write)
    {
        if (_current.Value is { InTransaction: true } session) session.Pending.Add(write);
        else write();
    }

    public Task<TResult> RunInAdhocSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                         CancellationToken? cancellationToken = null) =>
        RunAsync(new Session(false, true), callback, cancellationToken, commitOnSuccess: false);

    public Task<TResult> RunInSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                    CancellationToken? cancellationToken = null) =>
        RunAsync(new Session(true, true), callback, cancellationToken, commitOnSuccess: false);

    public Task<TResult> RunInManagedSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                           CancellationToken? cancellationToken = null) =>
        RunAsync(new Session(true, true), callback, cancellationToken, commitOnSuccess: true);

    public Task RunInManagedSessionAsync(SessionCallback callback, CancellationToken? cancellationToken = null) =>
        RunInManagedSessionAsync<bool>(async token => {
            await callback(token);
            return true;
        }, cancellationToken);

    public Task<TResult> RunInJoinedSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                          CancellationToken? cancellationToken = null)
    {
        if (_current.Value is not { InTransaction: true } parent) throw new NoActiveTransactionException();
        return RunAsync(new Session(true, false, parent.Pending), callback, cancellationToken, commitOnSuccess: false);
    }

    public Task<TResult> RunInJoinedOrManagedSessionAsync<TResult>(SessionCallback<TResult> callback,
                                                                   CancellationToken? cancellationToken = null) =>
        _current.Value is { InTransaction: true }
            ? RunInJoinedSessionAsync(callback, cancellationToken)
            : RunInManagedSessionAsync(callback, cancellationToken);

    public Task BeginAsync(CancellationToken? cancellationToken = null)
    {
        var session = Active();
        if (!session.Owns) throw new TransactionNotOwnedException();
        if (session.InTransaction) throw new TransactionAlreadyActiveException();
        session.InTransaction = true;
        return Task.CompletedTask;
    }

    public Task CommitAsync(bool suppressNoActiveTransactionException = false, CancellationToken? cancellationToken = null)
    {
        var session = Active();
        if (!session.Owns) throw new TransactionNotOwnedException();
        if (!session.InTransaction)
        {
            if (suppressNoActiveTransactionException) return Task.CompletedTask;
            throw new NoActiveTransactionException();
        }

        foreach (var write in session.Pending) write();
        session.Pending.Clear();
        session.InTransaction = false;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(Exception? reason = null, bool suppressNoActiveTransactionException = false,
                              CancellationToken? cancellationToken = null)
    {
        var session = Active();
        if (!session.Owns) throw new TransactionNotOwnedException();
        if (!session.InTransaction)
        {
            if (suppressNoActiveTransactionException) return Task.CompletedTask;
            throw new NoActiveTransactionException();
        }

        session.Pending.Clear();
        session.InTransaction = false;
        return Task.CompletedTask;
    }

    private Session Active() => _current.Value ?? throw new NoActiveSessionException();

    private async Task<TResult> RunAsync<TResult>(Session session, SessionCallback<TResult> callback,
                                                  CancellationToken? cancellationToken, bool commitOnSuccess)
    {
        if (_current.Value is { Owns: true } && session.Owns) throw new SessionAlreadyActiveException();
        cancellationToken?.ThrowIfCancellationRequested();

        var previous = _current.Value;
        _current.Value = session;
        try
        {
            var result = await callback(cancellationToken);
            if (commitOnSuccess && session.Owns) await CommitAsync(cancellationToken: cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            if (session.Owns && session.InTransaction) await RollbackAsync(exception);
            throw;
        }
        finally
        {
            if (session.Owns && session.InTransaction) session.Pending.Clear();
            _current.Value = previous;
        }
    }

    private sealed class Session(bool inTransaction, bool owns, List<Action>? pending = null)
    {
        public bool InTransaction { get; set; } = inTransaction;

        public bool Owns { get; } = owns;

        public List<Action> Pending { get; } = pending ?? [];
    }
}
