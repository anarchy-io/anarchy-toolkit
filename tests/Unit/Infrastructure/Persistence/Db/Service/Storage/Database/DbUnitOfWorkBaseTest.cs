using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;
using AwesomeAssertions;
using Moq;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Service.Storage.Database;

public class DbUnitOfWorkBaseTest
{
    private sealed class DummyConnectionPool()
        : DbConnectionPoolBase<DummyDbConnection>(DataStorageInstanceType.Writer, "Data Source=dummy")
    {
        protected override DummyDbConnection CreateConnection(string connectionString) => new(connectionString);
    }

    private sealed class DummyUnitOfWork(IDbConnectionPool<DummyDbConnection> pool)
        : DbUnitOfWorkBase<DummyDbConnection, DummyDbTransaction>(
            pool,
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<DummyDbConnection>>());

    private readonly DummyConnectionPool _pool = new();

    [Fact]
    public async Task RunInManagedSessionAsync_DisposesCommittedTransaction()
    {
        var sut = new DummyUnitOfWork(_pool);

        await sut.RunInManagedSessionAsync(_ => Task.FromResult(true));

        // The pool hands back the connection the session released
        var connection = (await _pool.ConnectAsync()).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => t.IsCommitted && t.IsDisposed);
    }

    [Fact]
    public async Task RunInSessionAsync_WithoutCommit_DisposesPendingTransaction()
    {
        var sut = new DummyUnitOfWork(_pool);

        await sut.RunInSessionAsync(_ => Task.FromResult(true));

        var connection = (await _pool.ConnectAsync()).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => !t.IsCommitted && t.IsDisposed);
    }

    [Fact]
    public async Task CommitAsync_AfterSessionFinished_ThrowsNoActiveTransaction()
    {
        var sut = new DummyUnitOfWork(_pool);
        await sut.RunInManagedSessionAsync(_ => Task.FromResult(true));

        var commit = () => sut.RunInAdhocSessionAsync(async _ => {
            await sut.CommitAsync();
            return true;
        });

        await commit.Should().ThrowAsync<Exception>()
            .Where(e => e.InnerException is NoActiveTransactionException);
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenBeginFails_PreservesTheBeginFailure()
    {
        var token = TestContext.Current.CancellationToken;
        var envelope = await _pool.ConnectAsync(token);
        var failure = new InvalidOperationException("begin failed");
        envelope.DataStorageConnection.BeginFailure = failure;
        await _pool.ReleaseAsync(envelope, token);
        var sut = new DummyUnitOfWork(_pool);

        var act = () => sut.RunInManagedSessionAsync(_ => Task.FromResult(true), token);

        var result = await act.Should().ThrowAsync<SessionCallbackException>();
        result.Which.InnerException.Should().BeOfType<TransactionBeginException>()
            .Which.InnerException.Should().BeSameAs(failure);
        envelope.DataStorageConnection.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenRollbackFails_PreservesTheCallbackFailure()
    {
        var token = TestContext.Current.CancellationToken;
        var envelope = await _pool.ConnectAsync(token);
        envelope.DataStorageConnection.RollbackFailure = new InvalidOperationException("rollback failed");
        await _pool.ReleaseAsync(envelope, token);
        var sut = new DummyUnitOfWork(_pool);
        var failure = new InvalidOperationException("callback failed");

        var act = () => sut.RunInManagedSessionAsync<bool>(_ => Task.FromException<bool>(failure), token);

        var result = await act.Should().ThrowAsync<SessionCallbackException>();
        result.Which.InnerException.Should().BeSameAs(failure);
        failure.Data["UnitOfWork.RollbackFailure"].Should().BeOfType<TransactionRollbackException>();
        envelope.DataStorageConnection.Transactions.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
        envelope.DataStorageConnection.IsDisposed.Should().BeTrue();
        (await _pool.ConnectAsync(token)).DataStorageConnection.Should().NotBeSameAs(envelope.DataStorageConnection);
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenReleaseFails_PreservesTheCallbackFailure()
    {
        var token = TestContext.Current.CancellationToken;
        var pool = new Mock<IDbConnectionPool<DummyDbConnection>>();
        pool.Setup(p => p.ConnectAsync(It.IsAny<CancellationToken?>())).Returns(() => _pool.ConnectAsync(token));
        pool.Setup(p => p.ReleaseAsync(It.IsAny<StorageConnectionEnvelopeBase<DummyDbConnection>>(), It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new InvalidOperationException("release failed"));
        var sut = new DummyUnitOfWork(pool.Object);
        var failure = new InvalidOperationException("callback failed");

        var act = () => sut.RunInManagedSessionAsync<bool>(_ => Task.FromException<bool>(failure), token);

        var result = await act.Should().ThrowAsync<SessionCallbackException>();
        result.Which.InnerException.Should().BeSameAs(failure);
        result.Which.Data["UnitOfWork.CleanupFailure"].Should().BeOfType<SessionFinishException>();
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenAnotherSessionIsActive_RejectsConcurrentUse()
    {
        var token = TestContext.Current.CancellationToken;
        var sut = new DummyUnitOfWork(_pool);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = sut.RunInManagedSessionAsync(async _ => {
            entered.SetResult();
            await release.Task.WaitAsync(token);
            return true;
        }, token);

        await entered.Task.WaitAsync(token);
        try
        {
            var concurrent = () => sut.RunInManagedSessionAsync(_ => Task.FromResult(true), token);
            await concurrent.Should().ThrowAsync<SessionAlreadyActiveException>();
        }
        finally
        {
            release.TrySetResult();
            await first;
        }
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenCancelled_RollsBackAndPropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var sut = new DummyUnitOfWork(_pool);

        var act = () => sut.RunInManagedSessionAsync<bool>(_ => {
            cancellation.Cancel();
            return Task.FromCanceled<bool>(cancellation.Token);
        }, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        var connection = (await _pool.ConnectAsync(TestContext.Current.CancellationToken)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => t.IsRolledBack && t.IsDisposed);
    }

    [Fact]
    public async Task RunInSessionAsync_WhenBeginningTwice_DoesNotReplaceTheTransaction()
    {
        var token = TestContext.Current.CancellationToken;
        var sut = new DummyUnitOfWork(_pool);

        var action = () => sut.RunInSessionAsync(async _ => {
            await sut.BeginAsync(token);
            return true;
        }, token);

        var result = await action.Should().ThrowAsync<SessionCallbackException>();
        result.Which.InnerException.Should().BeOfType<TransactionBeginException>();
        var connection = (await _pool.ConnectAsync(token)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenTransactionAndConnectionDisposalFail_PreservesBoth()
    {
        var token = TestContext.Current.CancellationToken;
        var envelope = await _pool.ConnectAsync(token);
        var transactionFailure = new InvalidOperationException("transaction disposal failed");
        var connectionFailure = new InvalidOperationException("connection disposal failed");
        envelope.DataStorageConnection.TransactionDisposeFailure = transactionFailure;
        envelope.DataStorageConnection.DisposeFailure = connectionFailure;
        await _pool.ReleaseAsync(envelope, token);
        var sut = new DummyUnitOfWork(_pool);

        var action = () => sut.RunInManagedSessionAsync(_ => Task.FromResult(true), token);

        var result = await action.Should().ThrowAsync<SessionFinishException>();
        result.Which.InnerException.Should().BeSameAs(transactionFailure);
        transactionFailure.Data["UnitOfWork.ConnectionCleanupFailure"].Should().BeSameAs(connectionFailure);
        (await sut.RunInAdhocSessionAsync(_ => Task.FromResult(true), token)).Should().BeTrue();
    }

    [Fact]
    public async Task RunInAdhocSessionAsync_WhenConnectionAcquisitionFails_AllowsLaterSessions()
    {
        var token = TestContext.Current.CancellationToken;
        var pool = new Mock<IDbConnectionPool<DummyDbConnection>>();
        pool.SetupSequence(p => p.ConnectAsync(It.IsAny<CancellationToken?>()))
            .ThrowsAsync(new InvalidOperationException("connection failed"))
            .Returns(() => _pool.ConnectAsync(token));
        pool.Setup(p => p.ReleaseAsync(It.IsAny<StorageConnectionEnvelopeBase<DummyDbConnection>>(), It.IsAny<CancellationToken?>()))
            .Returns((StorageConnectionEnvelopeBase<DummyDbConnection> envelope, CancellationToken? ct) => _pool.ReleaseAsync(envelope, ct));
        var sut = new DummyUnitOfWork(pool.Object);

        var action = () => sut.RunInAdhocSessionAsync(_ => Task.FromResult(true), token);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("connection failed");
        (await action()).Should().BeTrue();
    }
}
