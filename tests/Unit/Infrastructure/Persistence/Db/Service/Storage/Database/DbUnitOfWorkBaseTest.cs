using Anarchy.Toolkit.Core.Events.Local;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.LocalEvent.Service.Storage.UnitOfWork;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Testing.Telemetry;
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

    private sealed class DummyUnitOfWork(
        IDbConnectionPool<DummyDbConnection> pool,
        AppTelemetry? telemetry = null,
        ILocalEventBus? localEventBus = null,
        IAsyncFlowContext<StorageConnectionEnvelopeBase<DummyDbConnection>>? context = null)
        : DbUnitOfWorkBase<DummyDbConnection, DummyDbTransaction>(
            pool,
            context ?? new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<DummyDbConnection>>(), telemetry,
            localEventBus)
    {
        public StorageConnectionEnvelopeBase<DummyDbConnection> CurrentSession => ActiveConnectionStorage.Value;

        public Task FinishAsync() => FinishSessionAsync();
    }

    private sealed class RecordingLocalEventBus : ILocalEventBus
    {
        public List<ILocalEvent> Events { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];

        public Exception? CommittedFailure { get; init; }

        public Task PublishAsync<TEvent>(TEvent @event, bool absorbErrors = true, CancellationToken ct = default)
            where TEvent : ILocalEvent
        {
            Events.Add(@event);
            Tokens.Add(ct);
            return @event is CommittedUowTransactionLocalEvent && CommittedFailure is not null
                ? Task.FromException(CommittedFailure)
                : Task.CompletedTask;
        }
    }

    private readonly DummyConnectionPool _pool = new();

    private async Task<DummyDbConnection> PooledConnectionAsync(Action<DummyDbConnection> configure)
    {
        var token = TestContext.Current.CancellationToken;
        var envelope = await _pool.ConnectAsync(token);
        configure(envelope.DataStorageConnection);
        await _pool.ReleaseAsync(envelope, token);
        return envelope.DataStorageConnection;
    }

    private static AsyncLocalFlowContext<StorageConnectionEnvelopeBase<DummyDbConnection>> NewContext() => new();

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
    public async Task RunInSessionAsync_WithoutCommit_RollsBackPendingTransaction()
    {
        var bus = new RecordingLocalEventBus();
        var sut = new DummyUnitOfWork(_pool, localEventBus: bus);

        await sut.RunInSessionAsync(_ => Task.FromResult(true), TestContext.Current.CancellationToken);

        var connection = (await _pool.ConnectAsync(TestContext.Current.CancellationToken)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => !t.IsCommitted && t.IsRolledBack && t.IsDisposed);
        bus.Events.OfType<RolledBackUowTransactionLocalEvent>().Should().ContainSingle();
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
    public async Task RunInManagedSessionAsync_WhenBeginFails_PreservesTheBeginFailureWithoutRollingBack()
    {
        var token = TestContext.Current.CancellationToken;
        var failure = new InvalidOperationException("begin failed");
        var connection = await PooledConnectionAsync(c => c.BeginFailure = failure);
        var bus = new RecordingLocalEventBus();
        var sut = new DummyUnitOfWork(_pool, localEventBus: bus);

        var act = () => sut.RunInManagedSessionAsync(_ => Task.FromResult(true), token);

        var result = await act.Should().ThrowAsync<SessionCallbackException>();
        result.Which.InnerException.Should().BeOfType<TransactionBeginException>()
            .Which.InnerException.Should().BeSameAs(failure);
        failure.Data.Contains("UnitOfWork.RollbackFailure").Should().BeFalse();
        connection.Transactions.Should().BeEmpty();
        connection.IsDisposed.Should().BeTrue();
        bus.Events.Should().BeEmpty();
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
        result.Which.InnerException.Should().BeOfType<TransactionAlreadyActiveException>();
        var connection = (await _pool.ConnectAsync(token)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle().Which.Should()
            .Match<DummyDbTransaction>(t => t.IsRolledBack && t.IsDisposed);
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

    [Fact]
    public async Task RunInManagedSessionAsync_WhenCommitFails_RollsBackAndDiscardsTheConnection()
    {
        var token = TestContext.Current.CancellationToken;
        var failure = new InvalidOperationException("commit failed");
        var connection = await PooledConnectionAsync(c => c.CommitFailure = failure);
        var bus = new RecordingLocalEventBus();
        var sut = new DummyUnitOfWork(_pool, localEventBus: bus);

        var act = () => sut.RunInManagedSessionAsync(_ => Task.FromResult(true), token);

        var result = await act.Should().ThrowAsync<SessionCallbackException>();
        var commitFailure = result.Which.InnerException.Should().BeOfType<TransactionCommitException>().Which;
        commitFailure.InnerException.Should().BeSameAs(failure);
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => !t.IsCommitted && t.RollbackCount == 1 && t.IsDisposed);
        connection.IsDisposed.Should().BeTrue();
        bus.Events.Should().NotContain(e => e is CommittedUowTransactionLocalEvent);
        bus.Events.OfType<RolledBackUowTransactionLocalEvent>().Should().ContainSingle()
            .Which.Reason.Should().BeSameAs(commitFailure);
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenCommitAndRollbackFail_PreservesTheCommitFailure()
    {
        var token = TestContext.Current.CancellationToken;
        var connection = await PooledConnectionAsync(c => {
            c.CommitFailure = new InvalidOperationException("commit failed");
            c.RollbackFailure = new InvalidOperationException("rollback failed");
        });
        var sut = new DummyUnitOfWork(_pool);

        var act = () => sut.RunInManagedSessionAsync(_ => Task.FromResult(true), token);

        var result = await act.Should().ThrowAsync<SessionCallbackException>();
        var commitFailure = result.Which.InnerException.Should().BeOfType<TransactionCommitException>().Which;
        commitFailure.Data["UnitOfWork.RollbackFailure"].Should().BeOfType<TransactionRollbackException>()
            .Which.InnerException.Should().BeSameAs(connection.RollbackFailure);
        connection.Transactions.Should().ContainSingle().Which.RollbackCount.Should().Be(1);
        connection.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenCommittedEventFails_DoesNotReportACommitFailure()
    {
        var token = TestContext.Current.CancellationToken;
        var failure = new InvalidOperationException("handler failed");
        var bus = new RecordingLocalEventBus { CommittedFailure = failure };
        var sut = new DummyUnitOfWork(_pool, localEventBus: bus);

        var act = () => sut.RunInManagedSessionAsync(_ => Task.FromResult(true), token);

        var result = await act.Should().ThrowAsync<SessionCallbackException>();
        result.Which.InnerException.Should().BeSameAs(failure);
        var connection = (await _pool.ConnectAsync(token)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => t.IsCommitted && t.RollbackCount == 0 && t.IsDisposed);
        bus.Events.Should().NotContain(e => e is RolledBackUowTransactionLocalEvent);
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenCancelledBeforeCommit_RollsBackInsteadOfCommitting()
    {
        using var cancellation = new CancellationTokenSource();
        var bus = new RecordingLocalEventBus();
        var sut = new DummyUnitOfWork(_pool, localEventBus: bus);

        var act = () => sut.RunInManagedSessionAsync(_ => {
            cancellation.Cancel();
            return Task.FromResult(true);
        }, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        var connection = (await _pool.ConnectAsync(TestContext.Current.CancellationToken)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => !t.IsCommitted && t.IsRolledBack && t.IsDisposed);
        bus.Events.OfType<RolledBackUowTransactionLocalEvent>().Should().ContainSingle();
        bus.Tokens.Should().OnlyContain(t => !t.CanBeCanceled);
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenCallbackThrowsCancellationForAnotherToken_WrapsIt()
    {
        var token = TestContext.Current.CancellationToken;
        var sut = new DummyUnitOfWork(_pool);
        var failure = new OperationCanceledException("timed out");

        var act = () => sut.RunInManagedSessionAsync<bool>(_ => Task.FromException<bool>(failure), token);

        var result = await act.Should().ThrowAsync<SessionCallbackException>();
        result.Which.InnerException.Should().BeSameAs(failure);
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenCancelledWhileConnecting_PropagatesAndAllowsLaterSessions()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var pool = new Mock<IDbConnectionPool<DummyDbConnection>>();
        pool.SetupSequence(p => p.ConnectAsync(It.IsAny<CancellationToken?>()))
            .Returns(Task.FromCanceled<StorageConnectionEnvelopeBase<DummyDbConnection>>(cancellation.Token))
            .Returns(() => _pool.ConnectAsync(TestContext.Current.CancellationToken));
        pool.Setup(p => p.ReleaseAsync(It.IsAny<StorageConnectionEnvelopeBase<DummyDbConnection>>(), It.IsAny<CancellationToken?>()))
            .Returns((StorageConnectionEnvelopeBase<DummyDbConnection> envelope, CancellationToken? ct) => _pool.ReleaseAsync(envelope, ct));
        var sut = new DummyUnitOfWork(pool.Object);

        var act = () => sut.RunInManagedSessionAsync(_ => Task.FromResult(true), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        (await sut.RunInAdhocSessionAsync(_ => Task.FromResult(true), TestContext.Current.CancellationToken))
            .Should().BeTrue();
    }

    [Fact]
    public async Task RollbackAsync_WhenCancelled_AbandonsTheTransactionAndDiscardsTheConnection()
    {
        var token = TestContext.Current.CancellationToken;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var bus = new RecordingLocalEventBus();
        var sut = new DummyUnitOfWork(_pool, localEventBus: bus);
        DummyDbConnection? connection = null;

        var act = () => sut.RunInSessionAsync(async _ => {
            connection = sut.CurrentSession.DataStorageConnection;
            await sut.RollbackAsync(cancellationToken: cancellation.Token);
            return true;
        }, token);

        var result = await act.Should().ThrowAsync<SessionCallbackException>();
        result.Which.InnerException.Should().BeAssignableTo<OperationCanceledException>();
        connection!.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => t.RollbackCount == 0 && t.IsDisposed);
        connection.IsDisposed.Should().BeTrue();
        bus.Events.OfType<RolledBackUowTransactionLocalEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task RollbackAsync_WhenAlreadyRolledBack_DoesNotRollBackAgain()
    {
        var token = TestContext.Current.CancellationToken;
        var sut = new DummyUnitOfWork(_pool);

        await sut.RunInSessionAsync(async ct => {
            await sut.RollbackAsync(cancellationToken: ct);
            await sut.RollbackAsync(suppressNoActiveTransactionException: true, cancellationToken: ct);
            await sut.CommitAsync(suppressNoActiveTransactionException: true, cancellationToken: ct);

            var rollback = () => sut.RollbackAsync(cancellationToken: ct);
            await rollback.Should().ThrowAsync<NoActiveTransactionException>();
            var commit = () => sut.CommitAsync(cancellationToken: ct);
            await commit.Should().ThrowAsync<NoActiveTransactionException>();
            return true;
        }, token);

        var connection = (await _pool.ConnectAsync(token)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => t.RollbackCount == 1 && !t.IsCommitted && t.IsDisposed);
    }

    [Fact]
    public async Task RunInSessionAsync_WhenCommittedByTheCallback_DoesNotRollBack()
    {
        var token = TestContext.Current.CancellationToken;
        var sut = new DummyUnitOfWork(_pool);

        await sut.RunInSessionAsync(async ct => {
            await sut.CommitAsync(cancellationToken: ct);
            return true;
        }, token);

        var connection = (await _pool.ConnectAsync(token)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => t.IsCommitted && t.RollbackCount == 0 && t.IsDisposed);
    }

    [Fact]
    public async Task BeginAsync_OutsideASession_ThrowsNoActiveSession()
    {
        var token = TestContext.Current.CancellationToken;
        var sut = new DummyUnitOfWork(_pool);

        var begin = () => sut.BeginAsync(token);
        await begin.Should().ThrowAsync<NoActiveSessionException>();

        var commit = () => sut.CommitAsync(cancellationToken: token);
        await commit.Should().ThrowAsync<NoActiveTransactionException>();
        await sut.RollbackAsync(suppressNoActiveTransactionException: true, cancellationToken: token);
    }

    [Fact]
    public async Task CommitAsync_FromAnotherAsyncFlow_DoesNotTouchTheSessionTransaction()
    {
        var token = TestContext.Current.CancellationToken;
        var sut = new DummyUnitOfWork(_pool);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        DummyDbConnection? connection = null;
        var session = sut.RunInSessionAsync(async _ => {
            connection = sut.CurrentSession.DataStorageConnection;
            entered.SetResult();
            await release.Task.WaitAsync(token);
            return true;
        }, token);

        await entered.Task.WaitAsync(token);
        try
        {
            var commit = () => sut.CommitAsync(cancellationToken: token);
            await commit.Should().ThrowAsync<NoActiveTransactionException>();
            var rollback = () => sut.RollbackAsync(cancellationToken: token);
            await rollback.Should().ThrowAsync<NoActiveTransactionException>();
            var suppressedCommit = () => sut.CommitAsync(suppressNoActiveTransactionException: true, cancellationToken: token);
            await suppressedCommit.Should().ThrowAsync<NoActiveTransactionException>();
            var suppressedRollback = () => sut.RollbackAsync(suppressNoActiveTransactionException: true, cancellationToken: token);
            await suppressedRollback.Should().ThrowAsync<NoActiveTransactionException>();
            var begin = () => sut.BeginAsync(token);
            await begin.Should().ThrowAsync<NoActiveSessionException>();
        }
        finally
        {
            release.TrySetResult();
            await session;
        }

        connection!.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => !t.IsCommitted && t.RollbackCount == 1);
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenNested_RejectsTheInnerSessionAndKeepsTheOuterOne()
    {
        var token = TestContext.Current.CancellationToken;
        var context = NewContext();
        var sut = new DummyUnitOfWork(_pool, context: context);
        var other = new DummyUnitOfWork(_pool, context: context);

        await sut.RunInManagedSessionAsync(async ct => {
            var nested = () => sut.RunInManagedSessionAsync(_ => Task.FromResult(true), ct);
            await nested.Should().ThrowAsync<SessionAlreadyActiveException>();
            var nestedAdhoc = () => sut.RunInAdhocSessionAsync(_ => Task.FromResult(true), ct);
            await nestedAdhoc.Should().ThrowAsync<SessionAlreadyActiveException>();
            var nestedOther = () => other.RunInManagedSessionAsync(_ => Task.FromResult(true), ct);
            await nestedOther.Should().ThrowAsync<SessionAlreadyActiveException>();
            return true;
        }, token);

        var connection = (await _pool.ConnectAsync(token)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => t.IsCommitted && t.IsDisposed);
    }

    [Fact]
    public async Task RunInManagedSessionAsync_OnSeparateInstances_RunsConcurrentlyOnSeparateConnections()
    {
        var token = TestContext.Current.CancellationToken;
        var context = NewContext();
        var first = new DummyUnitOfWork(_pool, context: context);
        var second = new DummyUnitOfWork(_pool, context: context);
        var bothEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = 0;

        async Task<DummyDbConnection> RunAsync(DummyUnitOfWork unitOfWork) =>
            await unitOfWork.RunInManagedSessionAsync(async _ => {
                if (Interlocked.Increment(ref entered) == 2) bothEntered.SetResult();
                await bothEntered.Task.WaitAsync(token);
                return unitOfWork.CurrentSession.DataStorageConnection;
            }, token);

        var connections = await Task.WhenAll(RunAsync(first), RunAsync(second));

        connections[0].Should().NotBeSameAs(connections[1]);
        connections.Should().OnlyContain(c => c.Transactions.Single().IsCommitted);
    }

    [Fact]
    public async Task FinishSessionAsync_WhenCalledTwice_ReleasesTheConnectionOnce()
    {
        var token = TestContext.Current.CancellationToken;
        var pool = new Mock<IDbConnectionPool<DummyDbConnection>>();
        pool.Setup(p => p.ConnectAsync(It.IsAny<CancellationToken?>())).Returns(() => _pool.ConnectAsync(token));
        pool.Setup(p => p.ReleaseAsync(It.IsAny<StorageConnectionEnvelopeBase<DummyDbConnection>>(), It.IsAny<CancellationToken?>()))
            .Returns((StorageConnectionEnvelopeBase<DummyDbConnection> envelope, CancellationToken? ct) => _pool.ReleaseAsync(envelope, ct));
        var sut = new DummyUnitOfWork(pool.Object);
        DummyDbConnection? connection = null;

        await sut.RunInSessionAsync(async _ => {
            connection = sut.CurrentSession.DataStorageConnection;
            await sut.FinishAsync();
            await sut.FinishAsync();
            return true;
        }, token);

        pool.Verify(p => p.ReleaseAsync(It.IsAny<StorageConnectionEnvelopeBase<DummyDbConnection>>(), It.IsAny<CancellationToken?>()),
            Times.Once);
        connection!.Transactions.Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
        (await sut.RunInAdhocSessionAsync(_ => Task.FromResult(true), token)).Should().BeTrue();
    }

    [Fact]
    public async Task RunInJoinedSessionAsync_WithinTheCallerTransaction_SharesItsConnectionAndTransaction()
    {
        var token = TestContext.Current.CancellationToken;
        var context = NewContext();
        var sut = new DummyUnitOfWork(_pool, context: context);
        var other = new DummyUnitOfWork(_pool, context: context);

        await sut.RunInManagedSessionAsync(async ct => {
            var owned = sut.CurrentSession;
            (await sut.RunInJoinedSessionAsync(_ => Task.FromResult(sut.CurrentSession), ct)).Should().BeSameAs(owned);
            (await other.RunInJoinedSessionAsync(_ => Task.FromResult(other.CurrentSession), ct)).Should().BeSameAs(owned);
            return true;
        }, token);

        var connection = (await _pool.ConnectAsync(token)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => t.IsCommitted && t.RollbackCount == 0 && t.IsDisposed);
    }

    [Fact]
    public async Task RunInJoinedSessionAsync_CannotBeginCommitOrRollBackTheCallerTransaction()
    {
        var token = TestContext.Current.CancellationToken;
        var context = NewContext();
        var sut = new DummyUnitOfWork(_pool, context: context);
        var other = new DummyUnitOfWork(_pool, context: context);

        await sut.RunInManagedSessionAsync(async ct => {
            await sut.RunInJoinedSessionAsync(async joinedCt => {
                Func<Task>[] attempts = [
                    () => sut.CommitAsync(true, joinedCt),
                    () => sut.RollbackAsync(null, true, joinedCt),
                    () => sut.BeginAsync(joinedCt),
                    () => other.CommitAsync(true, joinedCt),
                    () => other.BeginAsync(joinedCt)
                ];
                foreach (var attempt in attempts)
                {
                    await attempt.Should().ThrowAsync<TransactionNotOwnedException>();
                }

                return true;
            }, ct);

            // The owner keeps control once the participant has returned
            sut.CurrentSession.IsInTransaction.Should().BeTrue();
            return true;
        }, token);

        var connection = (await _pool.ConnectAsync(token)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => t.IsCommitted && t.RollbackCount == 0);
    }

    [Fact]
    public async Task RunInJoinedSessionAsync_WhenTheParticipantFails_PropagatesUnchangedAndTheOwnerRollsBack()
    {
        var token = TestContext.Current.CancellationToken;
        var sut = new DummyUnitOfWork(_pool);
        var failure = new InvalidOperationException("aggregate failed");

        var act = () => sut.RunInManagedSessionAsync(
            async ct => await sut.RunInJoinedSessionAsync<bool>(_ => Task.FromException<bool>(failure), ct), token);

        var result = await act.Should().ThrowAsync<SessionCallbackException>();
        result.Which.InnerException.Should().BeSameAs(failure);
        var connection = (await _pool.ConnectAsync(token)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => !t.IsCommitted && t.RollbackCount == 1);
    }

    [Fact]
    public async Task RunInJoinedSessionAsync_WithoutACallerTransaction_ThrowsNoActiveTransaction()
    {
        var token = TestContext.Current.CancellationToken;
        var sut = new DummyUnitOfWork(_pool);

        var outside = () => sut.RunInJoinedSessionAsync(_ => Task.FromResult(true), token);
        await outside.Should().ThrowAsync<NoActiveTransactionException>();

        await sut.RunInAdhocSessionAsync(async ct => {
            var inAdhoc = () => sut.RunInJoinedSessionAsync(_ => Task.FromResult(true), ct);
            await inAdhoc.Should().ThrowAsync<NoActiveTransactionException>();
            var joinOrManage = () => sut.RunInJoinedOrManagedSessionAsync(_ => Task.FromResult(true), ct);
            await joinOrManage.Should().ThrowAsync<SessionAlreadyActiveException>();
            return true;
        }, token);
    }

    [Fact]
    public async Task RunInJoinedOrManagedSessionAsync_WithoutACallerTransaction_OwnsAManagedSession()
    {
        var token = TestContext.Current.CancellationToken;
        var sut = new DummyUnitOfWork(_pool);

        (await sut.RunInJoinedOrManagedSessionAsync(_ => Task.FromResult(true), token)).Should().BeTrue();

        var connection = (await _pool.ConnectAsync(token)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => t.IsCommitted && t.IsDisposed);
    }

    [Fact]
    public async Task RunInJoinedOrManagedSessionAsync_WithACallerTransaction_JoinsIt()
    {
        var token = TestContext.Current.CancellationToken;
        var sut = new DummyUnitOfWork(_pool);

        await sut.RunInManagedSessionAsync(async ct => {
            await sut.RunInJoinedOrManagedSessionAsync(async joinedCt => {
                var commit = () => sut.CommitAsync(cancellationToken: joinedCt);
                await commit.Should().ThrowAsync<TransactionNotOwnedException>();
                return true;
            }, ct);
            return true;
        }, token);

        var connection = (await _pool.ConnectAsync(token)).DataStorageConnection;
        connection.Transactions.Should().ContainSingle()
            .Which.Should().Match<DummyDbTransaction>(t => t.IsCommitted && t.RollbackCount == 0);
    }

    [Fact]
    public async Task RunInManagedSessionAsync_WhenCallbackFails_UsesTheInjectedTelemetry()
    {
        var token = TestContext.Current.CancellationToken;
        using var telemetry = new TestTelemetry();
        var sut = new DummyUnitOfWork(_pool, telemetry.Telemetry);

        var action = () => sut.RunInManagedSessionAsync<bool>(_ => Task.FromException<bool>(new InvalidOperationException("failed")), token);
        await action.Should().ThrowAsync<SessionCallbackException>();

        telemetry.Logs.Should().Contain(log => log.Message.Contains("Failed to execute session callback"));
        telemetry.Metrics.Metrics.Should().Contain(metric => metric.Kind == MetricKind.Duration
                                                            && metric.Name.EndsWith("start_async.execution_time")
                                                            && metric.HasTag("success", false));
    }
}
