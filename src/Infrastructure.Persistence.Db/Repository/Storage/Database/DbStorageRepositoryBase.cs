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
public abstract class DbStorageRepositoryBase<TDbConnection, TDialect>(
    IStoragePolicy<TDbConnection> dbPolicy,
    Observer observer)
    : DbRepositoryBase<TDbConnection>(dbPolicy, observer), IStorageRepository
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
    public abstract Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null);

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

    protected static async Task SavePointCreateAsync(StorageConnectionEnvelopeBase<TDbConnection> connEnvelope,
                                                     string savepointName,
                                                     CancellationToken? cancellationToken = null)
    {
        await ExecuteInTransactionAsync(connEnvelope, TDialect.SavepointCreateSql(savepointName), cancellationToken);
    }

    protected static async Task SavePointReleaseAsync(StorageConnectionEnvelopeBase<TDbConnection> connEnvelope,
                                                      string savepointName,
                                                      CancellationToken? cancellationToken = null)
    {
        await ExecuteInTransactionAsync(connEnvelope, TDialect.SavepointReleaseSql(savepointName), cancellationToken);
    }

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
