using Domain_Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;

/// <summary>
/// Writer for the generic transactional task outbox.
/// </summary>
public interface ITaskOutboxWriterRepository : IEventEnvelopeWriterRepository<Domain_Event>
{
    /// <summary>
    /// Sets a `PublishedAt` on the relevant tasks and persists it to the database.
    /// </summary>
    /// <param name="ids">List of IDs of tasks that should be updated</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of tasks updated</returns>
    Task<int> MarkAsPublishedAsync(IReadOnlyCollection<long> ids, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Sets a `PublishedAt` on the relevant task and persists it to the database.
    /// </summary>
    /// <param name="id">ID of the task that should be updated</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task MarkAsPublishedAsync(long id, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Removes old published tasks.
    /// </summary>
    /// <param name="retentionPeriod">The minimum time since the task publishing before it can be deleted</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of tasks deleted</returns>
    Task<int> CleanupPublishedAsync(TimeSpan? retentionPeriod = null, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Republishes, or publishes, tasks.
    /// </summary>
    /// <param name="entities">The events to (re)publish</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of tasks published</returns>
    Task<int> RepublishAllAsync(IReadOnlyCollection<Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event> entities, CancellationToken? cancellationToken = null);
}
