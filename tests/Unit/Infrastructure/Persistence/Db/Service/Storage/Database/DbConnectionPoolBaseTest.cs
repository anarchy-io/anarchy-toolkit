using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Service.Storage.Database;

public class DbConnectionPoolBaseTest
{
    private sealed class DummyConnectionPool()
        : DbConnectionPoolBase<DummyDbConnection>(DataStorageInstanceType.Reader, "Data Source=dummy")
    {
        public List<DummyDbConnection> Created { get; } = [];

        protected override DummyDbConnection CreateConnection(string connectionString)
        {
            var connection = new DummyDbConnection(connectionString);
            Created.Add(connection);
            return connection;
        }
    }

    [Fact]
    public async Task ConnectAsync_WhenPoolIsEmpty_CreatesAndOpensConnection()
    {
        var pool = new DummyConnectionPool();

        var envelope = await pool.ConnectAsync();

        pool.Created.Should().ContainSingle();
        envelope.DataStorageConnection.Should().BeSameAs(pool.Created[0]);
        envelope.DataStorageConnection.ConnectionString.Should().Be("Data Source=dummy");
        envelope.DataStorageConnection.OpenCount.Should().Be(1);
        envelope.InstanceType.Should().Be(DataStorageInstanceType.Reader);
        envelope.ConnectionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ConnectAsync_AfterReleasingOpenConnection_ReusesIt()
    {
        var pool = new DummyConnectionPool();
        var first = await pool.ConnectAsync();
        await pool.ReleaseAsync(first);

        var second = await pool.ConnectAsync();

        pool.Created.Should().ContainSingle();
        second.DataStorageConnection.Should().BeSameAs(first.DataStorageConnection);
        second.ConnectionId.Should().NotBe(first.ConnectionId);
    }

    [Fact]
    public async Task ReleaseAsync_WhenConnectionIsClosed_DisposesInsteadOfPooling()
    {
        var pool = new DummyConnectionPool();
        var first = await pool.ConnectAsync();
        await first.DataStorageConnection.CloseAsync();

        await pool.ReleaseAsync(first);
        var second = await pool.ConnectAsync();

        first.DataStorageConnection.IsDisposed.Should().BeTrue();
        second.DataStorageConnection.Should().NotBeSameAs(first.DataStorageConnection);
        pool.Created.Should().HaveCount(2);
    }

    [Fact]
    public async Task DestroyAsync_DisposesPooledConnections()
    {
        var pool = new DummyConnectionPool();
        var envelope = await pool.ConnectAsync();
        await pool.ReleaseAsync(envelope);

        await pool.DestroyAsync();

        envelope.DataStorageConnection.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_DisposesPooledConnections()
    {
        var pool = new DummyConnectionPool();
        var envelope = await pool.ConnectAsync();
        await pool.ReleaseAsync(envelope);

        await pool.DisposeAsync();

        envelope.DataStorageConnection.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task Dispose_DisposesPooledConnections()
    {
        var pool = new DummyConnectionPool();
        var envelope = await pool.ConnectAsync();
        await pool.ReleaseAsync(envelope);

        pool.Dispose();

        envelope.DataStorageConnection.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseAsync_AfterDispose_DisposesInsteadOfPooling()
    {
        var pool = new DummyConnectionPool();
        var envelope = await pool.ConnectAsync();

        await pool.DisposeAsync();
        await pool.ReleaseAsync(envelope);

        envelope.DataStorageConnection.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectAsync_AfterDispose_Throws()
    {
        var pool = new DummyConnectionPool();

        await pool.DisposeAsync();

        var connect = () => pool.ConnectAsync();
        await connect.Should().ThrowAsync<ObjectDisposedException>();
    }
}
