using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;

public abstract class DbReaderRepositoryBase<TDbConnection, TDialect, TPersistedEntity>(
    IStoragePolicy<TDbConnection> dbPolicy,
    Observer observer)
    : DbStorageRepositoryBase<TDbConnection, TDialect>(dbPolicy, observer),
      IStorageReaderRepository<TPersistedEntity>
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
    where TPersistedEntity : IPersistedEntity
{
    protected int GetAllMaxLimit => 500;

    public abstract Task<TPersistedEntity> GetByIdAsync(long id, CancellationToken? cancellationToken = null);

    public abstract Task<IReadOnlyCollection<TPersistedEntity>> GetAllAsync(int? limit = null, int? offset = null,
                                                                            CancellationToken? cancellationToken = null);

    protected async Task<TPersistedEntity> GetSingleByParamsAsync(
        string sqlFileName,
        object parameters,
        CancellationToken? cancellationToken = null)
    {
        TPersistedEntity? result;

        try
        {
            result = await DbPolicy.ExecuteAsync(
                async (connEnvelope, _) =>
                    await connEnvelope.DataStorageConnection.QuerySingleOrDefaultAsync<TPersistedEntity>(new CommandDefinition(
                        commandText: LoadSql(sqlFileName),
                        parameters: parameters,
                        cancellationToken: cancellationToken ?? CancellationToken.None)),
                sqlFileName,
                cancellationToken);
        }
        catch (Exception ex) when (TDialect.IsNoData(ex))
        {
            throw new EntityNotFoundException(
                $"No item of type {nameof(TPersistedEntity)} found based on filters {parameters}", ex);
        }

        return result ?? throw new EntityNotFoundException(
            $"No item of type {nameof(TPersistedEntity)} found based on filters {parameters}");
    }

    protected async Task<TPersistedEntity?> GetSingleOrDefaultByParamsAsync(string sqlFileName, object param,
                                                                             TPersistedEntity? defaultValue = default,
                                                                             CancellationToken? cancellationToken = null)
    {
        return await DbPolicy.ExecuteAsync(async (connEnvelope, _) => {
            var result =
                await connEnvelope.DataStorageConnection.QuerySingleOrDefaultAsync<TPersistedEntity>(new CommandDefinition(
                    commandText: LoadSql(sqlFileName),
                    parameters: param,
                    cancellationToken: cancellationToken ?? CancellationToken.None));

            return result ?? defaultValue;
        }, sqlFileName, cancellationToken);
    }

    protected async Task<IReadOnlyCollection<TPersistedEntity>> GetAllByParamsAsync(
        string sqlFileName,
        object param,
        CancellationToken? cancellationToken = null)
    {
        return await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                (await connEnvelope.DataStorageConnection.QueryAsync<TPersistedEntity>(new CommandDefinition(
                    commandText: LoadSql(sqlFileName),
                    parameters: param,
                    cancellationToken: cancellationToken ?? CancellationToken.None
                ))).ToArray(),
            sqlFileName,
            cancellationToken);
    }

    protected async Task<TPersistedEntity> GetByIdAsync(string sqlFileName, long id,
                                                        CancellationToken? cancellationToken = null)
    {
        return await GetSingleByParamsAsync(sqlFileName, new { id }, cancellationToken);
    }

    protected async Task<IReadOnlyCollection<TPersistedEntity>> GetAllAsync(
        string sqlFileName, int? limit = null, int? offset = null, CancellationToken? cancellationToken = null)
    {
        return await GetAllByParamsAsync(sqlFileName, QueryLimiterParams(limit, offset), cancellationToken);
    }

    protected object QueryLimiterParams(int? limit = null, int? offset = null)
    {
        return new { Limit = int.Min(limit ?? int.MaxValue, GetAllMaxLimit), Offset = offset is > 0 ? offset : 0 };
    }
}
