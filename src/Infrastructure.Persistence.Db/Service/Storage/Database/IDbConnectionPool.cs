using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;

/// <summary>
///     Interface of a database connection pool
/// </summary>
public interface IDbConnectionPool<TDbConnection> where TDbConnection : DbConnection
{
    /// <summary>
    ///     Returns one of the connections from the pool (establishes a new one if none are available or have been created yet)
    /// </summary>
    Task<StorageConnectionEnvelopeBase<TDbConnection>> ConnectAsync(CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Returns the connection to the pool so that it can be reused later
    /// </summary>
    Task ReleaseAsync(StorageConnectionEnvelopeBase<TDbConnection> connection,
                      CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Close the connection immediately, without waiting for any queued data (e.g.queries) to be sent.
    ///     No further events or callbacks will be triggered.
    /// </summary>
    Task DestroyAsync(CancellationToken? cancellationToken = null);
}
