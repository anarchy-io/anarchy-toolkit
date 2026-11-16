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

/// <summary>
/// Base for event envelope batch writers, adding resolution of the auxiliary type-table identifiers
/// (<see cref="GetIdForTypeAsync"/>) on top of <see cref="DbBatchWriterRepositoryBase{TDbConnection, TDialect, TNewEntity}"/>.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <typeparam name="TDialect">The provider dialect; <see cref="IDbDialect.CanSelectFromInsert"/> selects the get-or-insert strategy.</typeparam>
/// <typeparam name="TEventEnvelope">The event envelope type persisted by the repository.</typeparam>
/// <param name="dbPolicy">Policy used to execute database operations.</param>
/// <param name="observer">Observer used for logging and metrics.</param>
/// <remarks>
/// <para>
/// Type identifiers are cached process-wide in <see cref="EventWriterMultiAttributeIdCache"/>. On the first lookup
/// of a type table the cache is hydrated with <c>Common.{EventTypeTable}.Get.All</c>; missing names are then inserted
/// with <c>Common.{EventTypeTable}.Get.OrInsert</c> when the dialect can select from an insert, or with
/// <c>Common.{EventTypeTable}.Insert.IfMissing</c> followed by <c>Common.{EventTypeTable}.Get.ByName</c> otherwise.
/// </para>
/// <para>
/// Because an inserted type may be rolled back with its transaction, register
/// <see cref="LocalEventHandlers.RolledBackTransactionEventHandler"/> so the cache is cleared on rollback.
/// </para>
/// </remarks>
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

    /// <inheritdoc />
    /// <exception cref="EmptyArgumentException">
    /// Thrown when <paramref name="typeName"/> is <see langword="null"/> or empty and has to be inserted.
    /// </exception>
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
