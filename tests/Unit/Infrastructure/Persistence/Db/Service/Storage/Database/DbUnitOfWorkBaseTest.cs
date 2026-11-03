using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;
using AwesomeAssertions;

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
}
