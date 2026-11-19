using Anarchy.Toolkit.Core.Events.Entity.Event.Domain;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database.Record;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event;

/// <summary>
/// Provider-agnostic implementation of <see cref="IDomainEventReaderRepository"/>, reading persisted domain events
/// through the embedded <c>DomainEvent.Get.*</c> queries and mapping them via <see cref="DomainEventDbRecord"/>.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <typeparam name="TDialect">The provider dialect.</typeparam>
/// <param name="dbPolicy">Policy used to execute database operations.</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
/// <remarks>
/// Providers derive from this class (in their own namespace) and ship the queries
/// <c>DomainEvent.Get.ById</c>, <c>DomainEvent.Get.All</c>, <c>DomainEvent.Get.ByAggregateId</c>,
/// <c>DomainEvent.Get.ByTypeAggregate</c>, <c>DomainEvent.Get.ByTypeEvent</c> and
/// <c>Common.LatestSequenceValue.Get</c> as embedded resources.
/// </remarks>
public abstract class DbDomainEventReaderRepositoryBase<TDbConnection, TDialect>(
    IStoragePolicy<TDbConnection> dbPolicy,
    IAppTelemetry telemetry)
    : DbReaderRepositoryBase<TDbConnection, TDialect, PersistedEvent>(dbPolicy, telemetry),
      IDomainEventReaderRepository
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
    /// <summary>Name of the database sequence backing the record ids.</summary>
    protected virtual string IdSequenceName => "event_domain_metadata_id_seq";

    /// <summary>Returns the latest value of the <see cref="IdSequenceName"/> sequence.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The latest generated event identifier.</returns>
    public override async Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null)
    {
        return await GetLatestSequenceValueAsync(IdSequenceName, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<PersistedEvent> GetByIdAsync(long id, CancellationToken? cancellationToken = null)
    {
        var record = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QuerySingleAsync<DomainEventDbRecord>(new CommandDefinition(
                    commandText: LoadSql("DomainEvent.Get.ById"),
                    parameters: new { EventId = id },
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "DomainEvent.Get.ById", cancellationToken);

        return record.ToDomainEntity();
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<PersistedEvent>> GetAllAsync(
        int? limit = null, int? offset = null, CancellationToken? cancellationToken = null)
    {
        var records = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QueryAsync<DomainEventDbRecord>(new CommandDefinition(
                    commandText: LoadSql("DomainEvent.Get.All"),
                    parameters: QueryLimiterParams(limit, offset),
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "DomainEvent.Get.All", cancellationToken);

        return records.Select(r => r.ToDomainEntity()).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<PersistedEvent>> GetByAggregateIdAsync(string aggregateId,
                                                                                 CancellationToken? cancellationToken = null)
    {
        var records = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QueryAsync<DomainEventDbRecord>(new CommandDefinition(
                    commandText: LoadSql("DomainEvent.Get.ByAggregateId"),
                    parameters: new { AggregateId = aggregateId },
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "DomainEvent.Get.ByAggregateId", cancellationToken);

        return records.Select(r => r.ToDomainEntity()).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<PersistedEvent>> GetByTypeAggregateAsync(string typeAggregateName,
                                                                                   CancellationToken? cancellationToken = null)
    {
        var records = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QueryAsync<DomainEventDbRecord>(new CommandDefinition(
                    commandText: LoadSql("DomainEvent.Get.ByTypeAggregate"),
                    parameters: new { TypeAggregateName = typeAggregateName },
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "DomainEvent.Get.ByTypeAggregate", cancellationToken);

        return records.Select(r => r.ToDomainEntity()).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<PersistedEvent>> GetByTypeEventAsync(string typeEventName,
                                                                               CancellationToken? cancellationToken = null)
    {
        var records = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QueryAsync<DomainEventDbRecord>(new CommandDefinition(
                    commandText: LoadSql("DomainEvent.Get.ByTypeEvent"),
                    parameters: new { TypeEventName = typeEventName },
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "DomainEvent.Get.ByTypeEvent", cancellationToken);

        return records.Select(r => r.ToDomainEntity()).ToArray();
    }
}
