using Anarchy.Toolkit.Core.Events.Entity.Event.Domain;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database.Record;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event;

public abstract class DbTaskOutboxReaderRepositoryBase<TDbConnection, TDialect>(
    IStoragePolicy<TDbConnection> dbPolicy,
    Observer observer)
    : DbReaderRepositoryBase<TDbConnection, TDialect, PersistedEvent>(dbPolicy, observer),
      ITaskOutboxReaderRepository
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
    /// <summary>Name of the database sequence backing the record ids.</summary>
    protected virtual string IdSequenceName => "event_task_outbox_id_seq";

    public override async Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null)
    {
        return await GetLatestSequenceValueAsync(IdSequenceName, cancellationToken);
    }

    public override async Task<PersistedEvent> GetByIdAsync(long id, CancellationToken? cancellationToken = null)
    {
        var record = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QuerySingleAsync<TaskOutboxDbRecord>(new CommandDefinition(
                    commandText: LoadSql("TaskOutbox.Get.ById"),
                    parameters: new { TaskId = id },
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "TaskOutbox.Get.ById", cancellationToken);

        return record.ToDomainEntity();
    }

    public async Task<IReadOnlyCollection<PersistedEvent>> GetAllUnpublishedAsync(
        int? limit = null, int? offset = null, CancellationToken? cancellationToken = null)
    {
        var records = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QueryAsync<TaskOutboxDbRecord>(new CommandDefinition(
                    commandText: LoadSql("TaskOutbox.Get.All.Unpublished"),
                    parameters: QueryLimiterParams(limit, offset),
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "TaskOutbox.Get.All.Unpublished", cancellationToken);

        return records.Select(r => r.ToDomainEntity()).ToArray();
    }

    public override async Task<IReadOnlyCollection<PersistedEvent>> GetAllAsync(
        int? limit = null, int? offset = null, CancellationToken? cancellationToken = null)
    {
        var records = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QueryAsync<TaskOutboxDbRecord>(new CommandDefinition(
                    commandText: LoadSql("TaskOutbox.Get.All"),
                    parameters: QueryLimiterParams(limit, offset),
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "TaskOutbox.Get.All", cancellationToken);

        return records.Select(r => r.ToDomainEntity()).ToArray();
    }

    public async Task<IReadOnlyCollection<PersistedEvent>> GetByAggregateIdAsync(string aggregateId,
                                                                                 CancellationToken? cancellationToken = null)
    {
        var records = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QueryAsync<TaskOutboxDbRecord>(new CommandDefinition(
                    commandText: LoadSql("TaskOutbox.Get.ByAggregateId"),
                    parameters: new { AggregateId = aggregateId },
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "TaskOutbox.Get.ByAggregateId", cancellationToken);

        return records.Select(r => r.ToDomainEntity()).ToArray();
    }

    public async Task<IReadOnlyCollection<PersistedEvent>> GetByTypeAggregateAsync(string typeAggregateName,
                                                                                   CancellationToken? cancellationToken = null)
    {
        var records = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QueryAsync<TaskOutboxDbRecord>(new CommandDefinition(
                    commandText: LoadSql("TaskOutbox.Get.ByTypeAggregate"),
                    parameters: new { TypeAggregateName = typeAggregateName },
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "TaskOutbox.Get.ByTypeAggregate", cancellationToken);

        return records.Select(r => r.ToDomainEntity()).ToArray();
    }

    public async Task<IReadOnlyCollection<PersistedEvent>> GetByTypeTaskAsync(string typeTaskName,
                                                                              CancellationToken? cancellationToken = null)
    {
        var records = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QueryAsync<TaskOutboxDbRecord>(new CommandDefinition(
                    commandText: LoadSql("TaskOutbox.Get.ByTypeTask"),
                    parameters: new { TypeTaskName = typeTaskName },
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "TaskOutbox.Get.ByTypeTask", cancellationToken);

        return records.Select(r => r.ToDomainEntity()).ToArray();
    }
}
