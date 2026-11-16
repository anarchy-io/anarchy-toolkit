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
/// <typeparam name="TDbConnection">The type of database connection pooled.</typeparam>
/// <param name="instanceType">The instance type (reader or writer) the pool connects to.</param>
/// <param name="connectionString">Connection string used to create new connections.</param>
/// <remarks>
///     <para>
///     Idle connections are kept in a thread-safe bag without an upper bound; a new connection is created whenever
///     the bag is empty. Connections returned closed are disposed instead of pooled. The pool does not limit the
///     number of concurrent connections itself (the ADO.NET provider's own pooling still applies).
///     </para>
///     <para>
///     <see cref="DestroyAsync" /> closes idle connections but keeps the pool usable, whereas <see cref="Dispose" /> /
///     <see cref="DisposeAsync" /> also mark it disposed so that <see cref="ConnectAsync" /> throws.
///     </para>
/// </remarks>
/// <example>
///     <code language="csharp">
///     public sealed class MyConnectionPool(DataStorageInstanceType instanceType, string connectionString)
///         : DbConnectionPoolBase&lt;NpgsqlConnection&gt;(instanceType, connectionString)
///     {
///         protected override NpgsqlConnection CreateConnection(string connectionString) => new(connectionString);
///     }
///     </code>
/// </example>
public abstract class DbConnectionPoolBase<TDbConnection>(
    DataStorageInstanceType instanceType,
    string connectionString)
    : IDbConnectionPool<TDbConnection>, IDisposable, IAsyncDisposable
    where TDbConnection : DbConnection
{
    private readonly ConcurrentBag<TDbConnection> _pool = [];
    private volatile bool _isDisposed;

    /// <summary>The instance type (reader or writer) the pool connects to.</summary>
    protected DataStorageInstanceType InstanceType => instanceType;

    /// <summary>Creates a new, not yet opened, connection.</summary>
    /// <param name="connectionString">The connection string supplied to the pool.</param>
    /// <returns>The new connection.</returns>
    protected abstract TDbConnection CreateConnection(string connectionString);

    /// <summary>
    ///     Called every time a connection has been opened, before it is handed out. Providers use it to apply
    ///     session settings that cannot be expressed in the connection string.
    /// </summary>
    /// <param name="connection">The connection just opened.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the session has been prepared.</returns>
    protected virtual Task OnOpenedAsync(TDbConnection connection, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Reuses an idle connection when available (re-opening it, and calling <see cref="OnOpenedAsync" />, if it was
    ///     closed); otherwise creates and opens a new one.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
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

    /// <inheritdoc />
    /// <remarks>Open connections are returned to the pool; closed ones, or any after disposal, are disposed.</remarks>
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

    /// <summary>Disposes every idle connection. Connections currently handed out are not affected.</summary>
    /// <param name="cancellationToken">Not used.</param>
    /// <returns>A completed task.</returns>
    public Task DestroyAsync(CancellationToken? cancellationToken = null)
    {
        DisposePooledConnections();

        return Task.CompletedTask;
    }

    /// <summary>Marks the pool as disposed and disposes every idle connection.</summary>
    public void Dispose()
    {
        _isDisposed = true;
        DisposePooledConnections();
        GC.SuppressFinalize(this);
    }

    /// <summary>Marks the pool as disposed and asynchronously disposes every idle connection.</summary>
    /// <returns>A task that completes when the idle connections have been disposed.</returns>
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

    /// <summary>Wraps an opened connection in the envelope handed to callers.</summary>
    /// <param name="connection">The opened connection.</param>
    /// <returns>By default a new <see cref="DbConnectionEnvelope{TDbConnection}" />.</returns>
    protected virtual StorageConnectionEnvelopeBase<TDbConnection> WrapConnection(TDbConnection connection)
    {
        return new DbConnectionEnvelope<TDbConnection>(connection, instanceType);
    }
}
