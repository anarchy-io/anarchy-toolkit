using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Keeps opened connections of a single <see cref="DataStorageInstanceType" /> for reuse.
///     Providers only supply how a connection is created.
///     Disposing the pool closes every idle connection; connections released afterwards are closed instead of pooled.
/// </summary>
public abstract class DbConnectionPoolBase<TDbConnection>(
    DataStorageInstanceType instanceType,
    string connectionString)
    : IDbConnectionPool<TDbConnection>, IDisposable, IAsyncDisposable
    where TDbConnection : DbConnection
{
    private readonly ConcurrentBag<TDbConnection> _pool = [];
    private volatile bool _isDisposed;

    protected DataStorageInstanceType InstanceType => instanceType;

    protected abstract TDbConnection CreateConnection(string connectionString);

    /// <summary>
    ///     Called every time a connection has been opened, before it is handed out. Providers use it to apply
    ///     session settings that cannot be expressed in the connection string.
    /// </summary>
    protected virtual Task OnOpenedAsync(TDbConnection connection, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<StorageConnectionEnvelopeBase<TDbConnection>> ConnectAsync(
        CancellationToken? cancellationToken = null)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_pool.TryTake(out var conn))
        {
            if (conn.State == ConnectionState.Open)
            {
                return WrapConnection(conn);
            }

            await conn.OpenAsync(cancellationToken ?? CancellationToken.None);
            await OnOpenedAsync(conn, cancellationToken ?? CancellationToken.None);
            return WrapConnection(conn);
        }

        var newConn = CreateConnection(connectionString);
        await newConn.OpenAsync(cancellationToken ?? CancellationToken.None);
        await OnOpenedAsync(newConn, cancellationToken ?? CancellationToken.None);
        return WrapConnection(newConn);
    }

    public Task ReleaseAsync(StorageConnectionEnvelopeBase<TDbConnection> envelope,
                             CancellationToken? cancellationToken = null)
    {
        if (!_isDisposed && envelope.DataStorageConnection.State == ConnectionState.Open)
        {
            _pool.Add(envelope.DataStorageConnection);
        }
        else
        {
            envelope.DataStorageConnection.Dispose();
        }

        return Task.CompletedTask;
    }

    public Task DestroyAsync(CancellationToken? cancellationToken = null)
    {
        DisposePooledConnections();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _isDisposed = true;
        DisposePooledConnections();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;

        while (_pool.TryTake(out var conn))
        {
            await conn.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    private void DisposePooledConnections()
    {
        while (_pool.TryTake(out var conn))
        {
            conn.Dispose();
        }
    }

    protected virtual StorageConnectionEnvelopeBase<TDbConnection> WrapConnection(TDbConnection connection)
    {
        return new DbConnectionEnvelope<TDbConnection>(connection, instanceType);
    }
}
