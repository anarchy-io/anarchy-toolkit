using Anarchy.Toolkit.Core.Events.Envelopes;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;

/// <summary>
/// Writer for the generic transactional task outbox.
/// </summary>
/// <remarks>
/// <para>
/// Tasks are saved in the same transaction as the business changes that produced them (see
/// <see cref="Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.IUnitOfWork"/>), and later published by a
/// relay that reads them through <see cref="ITaskOutboxReaderRepository"/>, then calls
/// <see cref="MarkAsPublishedAsync(System.Collections.Generic.IReadOnlyCollection{long}, System.Nullable{System.Threading.CancellationToken})"/>.
/// Old published tasks are removed with <see cref="CleanupPublishedAsync"/>.
/// </para>
/// <para>
/// The provider-agnostic implementation is
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event.DbTaskOutboxWriterRepositoryBase{TDbConnection, TDialect}"/>.
/// </para>
/// </remarks>
public interface ITaskOutboxWriterRepository : IEventEnvelopeWriterRepository<DomainEventEnvelope>
{
    /// <summary>
    /// Sets a <c>PublishedAt</c> on the relevant tasks and persists it to the database.
    /// </summary>
    /// <param name="ids">List of IDs of tasks that should be updated</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of tasks updated</returns>
    Task<int> MarkAsPublishedAsync(IReadOnlyCollection<long> ids, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Sets a <c>PublishedAt</c> on the relevant task and persists it to the database.
    /// </summary>
    /// <param name="id">ID of the task that should be updated</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the update has been executed.</returns>
    Task MarkAsPublishedAsync(long id, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Removes old published tasks.
    /// </summary>
    /// <param name="retentionPeriod">
    /// The minimum time since the task publishing before it can be deleted. Implementations may apply a default
    /// (and cap) when <see langword="null"/>.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of tasks deleted</returns>
    Task<int> CleanupPublishedAsync(TimeSpan? retentionPeriod = null, CancellationToken? cancellationToken = null);

    /// <summary>
    /// Republishes, or publishes, tasks.
    /// </summary>
    /// <param name="entities">The events to (re)publish</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of tasks published</returns>
    Task<int> RepublishAllAsync(IReadOnlyCollection<DomainEventEnvelope> entities, CancellationToken? cancellationToken = null);
}
