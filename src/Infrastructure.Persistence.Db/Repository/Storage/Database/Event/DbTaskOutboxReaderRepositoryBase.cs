using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database.Record;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event;

/// <summary>
/// Provider-agnostic implementation of <see cref="ITaskOutboxReaderRepository"/>, reading outbox tasks through the
/// embedded <c>TaskOutbox.Get.*</c> queries and mapping them via <see cref="TaskOutboxDbRecord"/>.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <typeparam name="TDialect">The provider dialect.</typeparam>
/// <param name="dbPolicy">Policy used to execute database operations.</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
/// <remarks>
/// Providers derive from this class and ship the queries <c>TaskOutbox.Get.ById</c>, <c>TaskOutbox.Get.All</c>,
/// <c>TaskOutbox.Get.All.Unpublished</c>, <c>TaskOutbox.Get.ByAggregateId</c>, <c>TaskOutbox.Get.ByTypeAggregate</c>,
/// <c>TaskOutbox.Get.ByTypeTask</c> and <c>Common.LatestSequenceValue.Get</c> as embedded resources.
/// </remarks>
public abstract class DbTaskOutboxReaderRepositoryBase<TDbConnection, TDialect>(
    IStoragePolicy<TDbConnection> dbPolicy,
    AppTelemetry telemetry)
    : DbReaderRepositoryBase<TDbConnection, TDialect, PersistedDomainEvent>(dbPolicy, telemetry),
      ITaskOutboxReaderRepository
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
    /// <summary>Name of the database sequence backing the record ids.</summary>
    protected virtual string IdSequenceName => "event_task_outbox_id_seq";

    /// <summary>Returns the latest value of the <see cref="IdSequenceName"/> sequence.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The latest generated task identifier.</returns>
    public override async Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null)
    {
        return await GetLatestSequenceValueAsync(IdSequenceName, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<PersistedDomainEvent> GetByIdAsync(long id, CancellationToken? cancellationToken = null)
    {
        var record = await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.QuerySingleAsync<TaskOutboxDbRecord>(new CommandDefinition(
                    commandText: LoadSql("TaskOutbox.Get.ById"),
                    parameters: new { TaskId = id },
                    cancellationToken: cancellationToken ?? CancellationToken.None)), "TaskOutbox.Get.ById", cancellationToken);

        return record.ToDomainEntity();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<PersistedDomainEvent>> GetAllUnpublishedAsync(
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

    /// <inheritdoc />
    public override async Task<IReadOnlyCollection<PersistedDomainEvent>> GetAllAsync(
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

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<PersistedDomainEvent>> GetByAggregateIdAsync(string aggregateId,
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

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<PersistedDomainEvent>> GetByTypeAggregateAsync(string typeAggregateName,
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

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<PersistedDomainEvent>> GetByTypeTaskAsync(string typeTaskName,
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
