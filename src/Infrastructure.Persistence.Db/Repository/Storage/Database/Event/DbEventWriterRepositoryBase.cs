using Anarchy.Toolkit.Core.Events.Entity.Event;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.ValueSet.Storage.Database;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event;

public abstract class DbEventWriterRepositoryBase<TDbConnection, TDialect, TEventEnvelope>(
    IStoragePolicy<TDbConnection> dbPolicy,
    Observer observer) :
    DbBatchWriterRepositoryBase<TDbConnection, TDialect, TEventEnvelope>(dbPolicy, observer),
    IEventEnvelopeWriterRepository<TEventEnvelope>
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
    where TEventEnvelope : IEventEnvelope
{
    private async Task<IReadOnlyCollection<EventWriterMultiAttributeIdCache.Dto>> TypeIdsHydrateAsync(
        EventTypeTable typeTable,
        CancellationToken? cancellationToken = null)
    {
        var sqlFile = $"Common.{typeTable.ToString()}.Get.All";

        return (await DbPolicy.ExecuteAsync(
            async (connection, _) =>
                await connection.DataStorageConnection.QueryAsync<EventWriterMultiAttributeIdCache.Dto>(
                    new CommandDefinition(
                        commandText: LoadSql(sqlFile),
                        cancellationToken: cancellationToken ?? CancellationToken.None)),
            sqlFile,
            cancellationToken)).ToArray();
    }

    private async Task<int> TypeIdsInsertSingleAsync(EventTypeTable typeTable, string typeName,
                                                     CancellationToken? cancellationToken = null)
    {
        if (typeName.IsNullOrEmpty())
        {
            throw new EmptyArgumentException(nameof(typeName));
        }

        var parameters = new { Name = typeName };

        if (TDialect.CanSelectFromInsert)
        {
            var sqlFile = $"Common.{typeTable.ToString()}.Get.OrInsert";

            return await DbPolicy.ExecuteAsync(
                async (connection, _) =>
                    await connection.DataStorageConnection.QuerySingleAsync<int>(
                        new CommandDefinition(
                            commandText: LoadSql(sqlFile),
                            parameters: parameters,
                            cancellationToken: cancellationToken ?? CancellationToken.None)),
                sqlFile,
                cancellationToken);
        }

        var insertSqlFile = $"Common.{typeTable.ToString()}.Insert.IfMissing";
        var getSqlFile = $"Common.{typeTable.ToString()}.Get.ByName";

        return await DbPolicy.ExecuteAsync(
            async (connection, _) => {
                await connection.DataStorageConnection.ExecuteAsync(new CommandDefinition(
                    commandText: LoadSql(insertSqlFile),
                    parameters: parameters,
                    cancellationToken: cancellationToken ?? CancellationToken.None));

                return await connection.DataStorageConnection.QuerySingleAsync<int>(new CommandDefinition(
                    commandText: LoadSql(getSqlFile),
                    parameters: parameters,
                    cancellationToken: cancellationToken ?? CancellationToken.None));
            },
            insertSqlFile,
            cancellationToken);
    }

    public async Task<int> GetIdForTypeAsync(EventTypeTable typeTable, string typeName,
                                             CancellationToken? cancellationToken = null)
    {
        return await EventWriterMultiAttributeIdCache.GetOrAddAsync(
            typeTable,
            typeName,
            TypeIdsHydrateAsync,
            TypeIdsInsertSingleAsync,
            cancellationToken);
    }
}
