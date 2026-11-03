using Anarchy.Toolkit.Core.Events.Entity.Event.Domain;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database.Record;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event;

public abstract class DbDomainEventReaderRepositoryBase<TDbConnection, TDialect>(
    IStoragePolicy<TDbConnection> dbPolicy,
    Observer observer)
    : DbReaderRepositoryBase<TDbConnection, TDialect, PersistedEvent>(dbPolicy, observer),
      IDomainEventReaderRepository
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
    /// <summary>Name of the database sequence backing the record ids.</summary>
    protected virtual string IdSequenceName => "event_domain_metadata_id_seq";

    public override async Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null)
    {
        return await GetLatestSequenceValueAsync(IdSequenceName, cancellationToken);
    }

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
