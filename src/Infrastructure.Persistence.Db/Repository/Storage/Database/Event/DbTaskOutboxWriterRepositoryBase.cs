using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database.Input;
using Dapper;
using System.Data.Common;
using Domain_Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event;

public abstract class DbTaskOutboxWriterRepositoryBase<TDbConnection, TDialect>(
    IStoragePolicy<TDbConnection> dbPolicy,
    Observer observer
) : DbEventWriterRepositoryBase<TDbConnection, TDialect, Domain_Event>(dbPolicy, observer),
    ITaskOutboxWriterRepository
    where TDbConnection : DbConnection
    where TDialect : IDbDialect
{
    private readonly TimeSpan _minimumRetentionPeriod = new(30, 0, 0, 0);

    /// <summary>Name of the database sequence backing the record ids.</summary>
    protected virtual string IdSequenceName => "event_task_outbox_id_seq";

    public override async Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null)
    {
        return await GetLatestSequenceValueAsync(IdSequenceName, cancellationToken);
    }

    public override Task<int> SaveAllAsync(IReadOnlyCollection<Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event> entities,
                                           CancellationToken? cancellationToken = null)
    {
        return SaveAllAsync("TaskOutbox.Save.All", entities, cancellationToken);
    }

    public Task<int> RepublishAllAsync(IReadOnlyCollection<Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event> entities,
                                       CancellationToken? cancellationToken = null)
    {
        return SaveAllAsync("TaskOutbox.Republish.All", entities, cancellationToken);
    }

    protected override async Task<object> ToSaveInput(Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event newEntity,
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

    public async Task MarkAsPublishedAsync(long id, CancellationToken? cancellationToken = null)
    {
        await MarkAsPublishedAsync([id], cancellationToken);
    }

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
