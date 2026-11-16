using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Interface of a database connection pool
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection pooled.</typeparam>
/// <remarks>
///     Used by <see cref="DbPolicyBase{TDbConnection}" /> for standalone operations and by
///     <see cref="DbUnitOfWorkBase{TDbConnection, TDbTransaction}" /> for sessions. The provider-agnostic
///     implementation is <see cref="DbConnectionPoolBase{TDbConnection}" />.
/// </remarks>
public interface IDbConnectionPool<TDbConnection> where TDbConnection : DbConnection
{
    /// <summary>
    ///     Returns one of the connections from the pool (establishes a new one if none are available or have been created yet)
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An envelope wrapping an opened connection.</returns>
    Task<StorageConnectionEnvelopeBase<TDbConnection>> ConnectAsync(CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Returns the connection to the pool so that it can be reused later
    /// </summary>
    /// <param name="connection">The envelope obtained from <see cref="ConnectAsync" />.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the connection has been released.</returns>
    Task ReleaseAsync(StorageConnectionEnvelopeBase<TDbConnection> connection,
                      CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Close the connection immediately, without waiting for any queued data (e.g.queries) to be sent.
    ///     No further events or callbacks will be triggered.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the connections have been closed.</returns>
    Task DestroyAsync(CancellationToken? cancellationToken = null);
}
