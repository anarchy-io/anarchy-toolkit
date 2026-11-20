using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database.Input;
using Dapper;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event;

/// <summary>
/// Provider-agnostic implementation of <see cref="ITaskOutboxWriterRepository"/>, persisting domain events as outbox
/// tasks and managing their publication state.
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection.</typeparam>
/// <typeparam name="TDialect">The provider dialect.</typeparam>
/// <param name="dbPolicy">Policy used to execute database operations.</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
/// <remarks>
/// Providers derive from this class and ship the queries <c>TaskOutbox.Save.All</c>, <c>TaskOutbox.Republish.All</c>,
/// <c>TaskOutbox.Published.Mark</c> (receives <c>Ids</c>), <c>TaskOutbox.Published.Cleanup</c> (receives
/// <c>PublishedUntil</c>), <c>Common.LatestSequenceValue.Get</c> and the type-table queries described on
/// <see cref="DbEventWriterRepositoryBase{TDbConnection, TDialect, TEventEnvelope}"/>. Each event is converted into a
/// <see cref="TaskOutboxDbInput"/>, using the event type as task type.
/// </remarks>
public abstract class DbTaskOutboxWriterRepositoryBase<TDbConnection, TDialect>(
    IStoragePolicy<TDbConnection> dbPolicy,
    AppTelemetry telemetry
) : DbEventWriterRepositoryBase<TDbConnection, TDialect, DomainEventEnvelope>(dbPolicy, telemetry),
    ITaskOutboxWriterRepository
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
    private readonly TimeSpan _minimumRetentionPeriod = new(30, 0, 0, 0);

    /// <summary>Name of the database sequence backing the record ids.</summary>
    protected virtual string IdSequenceName => "event_task_outbox_id_seq";

    /// <summary>Returns the latest value of the <see cref="IdSequenceName"/> sequence.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The latest generated task identifier.</returns>
    public override async Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null)
    {
        return await GetLatestSequenceValueAsync(IdSequenceName, cancellationToken);
    }

    /// <summary>Saves the events as outbox tasks with the <c>TaskOutbox.Save.All</c> query in a single batch command.</summary>
    /// <param name="entities">The events to save.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of affected rows.</returns>
    public override Task<int> SaveAllAsync(IReadOnlyCollection<DomainEventEnvelope> entities,
                                           CancellationToken? cancellationToken = null)
    {
        return SaveAllAsync("TaskOutbox.Save.All", entities, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>Executes the <c>TaskOutbox.Republish.All</c> query in a single batch command.</remarks>
    public Task<int> RepublishAllAsync(IReadOnlyCollection<DomainEventEnvelope> entities,
                                       CancellationToken? cancellationToken = null)
    {
        return SaveAllAsync("TaskOutbox.Republish.All", entities, cancellationToken);
    }

    /// <summary>
    /// Converts an event into a <see cref="TaskOutboxDbInput"/>, resolving the aggregate and task type identifiers and
    /// serializing the payload to JSON.
    /// </summary>
    /// <param name="newEntity">The event to convert.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The <see cref="TaskOutboxDbInput"/> for the event.</returns>
    protected override async Task<object> ToSaveInput(DomainEventEnvelope newEntity,
                                                      CancellationToken? cancellationToken = null)
    {
        return new TaskOutboxDbInput {
            TypeAggregateId = await GetIdForTypeAsync(EventTypeTable.TypeAggregate, newEntity.TypeAggregate, cancellationToken),
            AggregateId = newEntity.AggregateId,
            TypeTaskId = await GetIdForTypeAsync(EventTypeTable.TypeTask, newEntity.TypeEvent, cancellationToken),
            Payload = newEntity.Payload?.ToJson(),
            EventHash = HashHelper.HashWithSha1(new {
                newEntity.TypeAggregate,
                newEntity.AggregateId,
                newEntity.Payload,
                newEntity.TypeEvent
            }),
            CreatedAt = newEntity.Moment
        };
    }

    /// <inheritdoc />
    public async Task MarkAsPublishedAsync(long id, CancellationToken? cancellationToken = null)
    {
        await MarkAsPublishedAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> MarkAsPublishedAsync(IReadOnlyCollection<long> ids,
                                                CancellationToken? cancellationToken = null)
    {
        return await DbPolicy.ExecuteAsync(
            async (connEnvelope, callbackCancellationToken) => {
                var command = new CommandDefinition(
                    commandText: LoadSql("TaskOutbox.Published.Mark"),
                    parameters: new { Ids = ids },
                    cancellationToken: cancellationToken ?? callbackCancellationToken ?? CancellationToken.None);

                return await connEnvelope.DataStorageConnection.ExecuteAsync(command);
            },
            "TaskOutbox.Published.Mark",
            cancellationToken);
    }

    /// <summary>Removes published tasks older than the effective retention period.</summary>
    /// <param name="retentionPeriod">
    /// Requested retention period. The effective retention is the smaller of this value and 30 days; when
    /// <see langword="null"/> it is 30 days.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of tasks deleted.</returns>
    public async Task<int> CleanupPublishedAsync(TimeSpan? retentionPeriod = null,
                                                 CancellationToken? cancellationToken = null)
    {
        var effectiveRetention = retentionPeriod is { } period && period < _minimumRetentionPeriod
            ? period
            : _minimumRetentionPeriod;

        return await DbPolicy.ExecuteAsync(
            async (connEnvelope, _) =>
                await connEnvelope.DataStorageConnection.ExecuteAsync(new CommandDefinition(
                    commandText: LoadSql("TaskOutbox.Published.Cleanup"),
                    parameters: new { PublishedUntil = DateTimeOffset.UtcNow - effectiveRetention },
                    cancellationToken: cancellationToken ?? CancellationToken.None)),
            "TaskOutbox.Published.Cleanup",
            cancellationToken);
    }
}
