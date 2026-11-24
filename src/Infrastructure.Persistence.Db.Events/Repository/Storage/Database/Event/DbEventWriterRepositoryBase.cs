using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Core.Service.Repository.Storage.Event;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Core.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event;

/// <summary>
/// Base for event envelope batch writers, adding resolution of the auxiliary type-table identifiers
/// (<see cref="GetIdForTypeAsync"/>) on top of <see cref="DbBatchWriterRepositoryBase{TDbConnection, TDialect, TNewEntity}"/>.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <typeparam name="TDialect">The provider dialect; <see cref="IDbDialect.CanSelectFromInsert"/> selects the get-or-insert strategy.</typeparam>
/// <typeparam name="TEventEnvelope">The event envelope type persisted by the repository.</typeparam>
/// <param name="dbPolicy">Policy used to execute database operations.</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
/// <remarks>
/// <para>
/// Each lookup through <see cref="EventWriterMultiAttributeIdCache"/> consults the caller's database and active
/// transaction, if any, using <c>Common.{EventTypeTable}.Get.All</c>. Missing names are resolved with
/// <c>Common.{EventTypeTable}.Get.OrInsert</c> when the dialect can select from an insert, or with
/// <c>Common.{EventTypeTable}.Insert.IfMissing</c> followed by <c>Common.{EventTypeTable}.Get.ByName</c> otherwise.
/// </para>
/// <para>
/// The callbacks do not expose database identity or transaction commit outcome, so a shared cache could mix databases
/// or publish uncommitted identifiers. No identifiers are shared between lookups, and rollback-handler registration is
/// not required for this resolution path. Concurrent first-time registration relies on provider get-or-insert SQL and
/// database unique constraints to coordinate inserts safely.
/// </para>
/// <para>
/// Concrete domain event writers provide their own SQL and must persist <see cref="IEventEnvelope.CustomAttributes"/>
/// as JSON object text. Queries that read the event back must return that text as <c>CustomAttributes</c>.
/// </para>
/// </remarks>
public abstract class DbEventWriterRepositoryBase<TDbConnection, TDialect, TEventEnvelope>(
    IStoragePolicy<TDbConnection> dbPolicy,
     AppTelemetry telemetry) :
    DbBatchWriterRepositoryBase<TDbConnection, TDialect, TEventEnvelope>(dbPolicy, telemetry),
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
