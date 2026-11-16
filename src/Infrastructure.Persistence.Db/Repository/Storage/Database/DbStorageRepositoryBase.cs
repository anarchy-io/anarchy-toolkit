using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;

/// <summary>
///     Base of the storage repositories: sequence lookup and transaction savepoints, expressed through
///     <typeparamref name="TDialect" />.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <typeparam name="TDialect">The provider dialect supplying the savepoint SQL.</typeparam>
/// <param name="dbPolicy">Policy used to execute database operations.</param>
/// <param name="observer">Observer used for logging and metrics.</param>
public abstract class DbStorageRepositoryBase<TDbConnection, TDialect>(
    IStoragePolicy<TDbConnection> dbPolicy,
    Observer observer)
    : DbRepositoryBase<TDbConnection>(dbPolicy, observer), IStorageRepository
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
    /// <summary>Gets the latest identifier generated for the entities stored by this repository.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The latest generated identifier.</returns>
    public abstract Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Gets the latest value of a database sequence using the <c>Common.LatestSequenceValue.Get</c> embedded query
    ///     (which receives a <c>sequenceName</c> parameter).
    /// </summary>
    /// <param name="sequenceName">Name of the sequence.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The latest sequence value.</returns>
    protected async Task<long> GetLatestSequenceValueAsync(string sequenceName,
                                                           CancellationToken? cancellationToken = null)
    {
        return await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QuerySingleAsync<long>(new CommandDefinition(
                    commandText: LoadSql("Common.LatestSequenceValue.Get"),
                    parameters: new {
                        sequenceName
                    },
                    cancellationToken: cancellationToken ?? CancellationToken.None)),
            "Common.LatestSequenceValue.Get",
            cancellationToken);
    }

    /// <summary>Creates a savepoint using <see cref="IDbDialect.SavepointCreateSql" />.</summary>
    /// <param name="connEnvelope">The connection envelope to execute on.</param>
    /// <param name="savepointName">Name of the savepoint.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     No-op when the connection is not in a transaction or the dialect returns <see langword="null" /> SQL.
    /// </remarks>
    protected static async Task SavePointCreateAsync(StorageConnectionEnvelopeBase<TDbConnection> connEnvelope,
                                                     string savepointName,
                                                     CancellationToken? cancellationToken = null)
    {
        await ExecuteInTransactionAsync(connEnvelope, TDialect.SavepointCreateSql(savepointName), cancellationToken);
    }

    /// <summary>Releases a savepoint using <see cref="IDbDialect.SavepointReleaseSql" />.</summary>
    /// <param name="connEnvelope">The connection envelope to execute on.</param>
    /// <param name="savepointName">Name of the savepoint.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     No-op when the connection is not in a transaction or the dialect returns <see langword="null" /> SQL.
    /// </remarks>
    protected static async Task SavePointReleaseAsync(StorageConnectionEnvelopeBase<TDbConnection> connEnvelope,
                                                      string savepointName,
                                                      CancellationToken? cancellationToken = null)
    {
        await ExecuteInTransactionAsync(connEnvelope, TDialect.SavepointReleaseSql(savepointName), cancellationToken);
    }

    /// <summary>
    ///     Rolls back to a savepoint (<see cref="IDbDialect.SavepointRollbackSql" />) and then releases it
    ///     (<see cref="IDbDialect.SavepointReleaseSql" />), keeping the surrounding transaction usable.
    /// </summary>
    /// <param name="connEnvelope">The connection envelope to execute on.</param>
    /// <param name="savepointName">Name of the savepoint.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     No-op when the connection is not in a transaction or the dialect returns <see langword="null" /> SQL.
    /// </remarks>
    protected static async Task SavePointRollbackAndReleaseAsync(
        StorageConnectionEnvelopeBase<TDbConnection> connEnvelope, string savepointName,
        CancellationToken? cancellationToken = null)
    {
        await ExecuteInTransactionAsync(connEnvelope, TDialect.SavepointRollbackSql(savepointName), cancellationToken);
        await ExecuteInTransactionAsync(connEnvelope, TDialect.SavepointReleaseSql(savepointName), cancellationToken);
    }

    private static async Task ExecuteInTransactionAsync(StorageConnectionEnvelopeBase<TDbConnection> connEnvelope,
                                                        string? sql,
                                                        CancellationToken? cancellationToken)
    {
        if (connEnvelope.IsInTransaction && sql is not null)
        {
            await connEnvelope.DataStorageConnection.ExecuteAsync(new CommandDefinition(
                commandText: sql,
                cancellationToken: cancellationToken ?? CancellationToken.None));
        }
    }
}
