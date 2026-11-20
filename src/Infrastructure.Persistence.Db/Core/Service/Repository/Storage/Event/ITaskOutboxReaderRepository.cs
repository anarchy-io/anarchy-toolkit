using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;

/// <summary>
/// Read-side repository over the transactional task outbox, returning tasks as <see cref="PersistedDomainEvent"/>
/// instances.
/// </summary>
/// <remarks>
/// Typically used by an outbox relay: it polls <see cref="GetAllUnpublishedAsync"/>, publishes the tasks and then
/// marks them via <see cref="ITaskOutboxWriterRepository.MarkAsPublishedAsync(System.Collections.Generic.IReadOnlyCollection{long}, System.Nullable{System.Threading.CancellationToken})"/>.
/// The provider-agnostic implementation is
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event.DbTaskOutboxReaderRepositoryBase{TDbConnection, TDialect}"/>.
/// </remarks>
public interface ITaskOutboxReaderRepository : IStorageReaderRepository<PersistedDomainEvent>
{
    /// <summary>Gets every outbox task belonging to the aggregate with the given identifier.</summary>
    /// <param name="aggregateId">Identifier of the aggregate instance.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching tasks; an empty collection when none exist.</returns>
    Task<IReadOnlyCollection<PersistedDomainEvent>> GetByAggregateIdAsync(string aggregateId,
                                                                    CancellationToken? cancellationToken = null);
    /// <summary>Gets every outbox task belonging to aggregates of the given type.</summary>
    /// <param name="typeAggregateName">Name of the aggregate type.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching tasks; an empty collection when none exist.</returns>
    Task<IReadOnlyCollection<PersistedDomainEvent>> GetByTypeAggregateAsync(string typeAggregateName,
                                                                      CancellationToken? cancellationToken = null);
    /// <summary>Gets every outbox task of the given task type.</summary>
    /// <param name="typeTaskName">Name of the task type.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching tasks; an empty collection when none exist.</returns>
    Task<IReadOnlyCollection<PersistedDomainEvent>> GetByTypeTaskAsync(string typeTaskName,
                                                                 CancellationToken? cancellationToken = null);
    /// <summary>Gets a page of outbox tasks that have not been published yet.</summary>
    /// <param name="limit">Maximum number of tasks to return; implementations may cap it.</param>
    /// <param name="offset">Number of tasks to skip; <see langword="null"/> or non-positive values mean zero.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The unpublished tasks; an empty collection when none exist.</returns>
    Task<IReadOnlyCollection<PersistedDomainEvent>> GetAllUnpublishedAsync(int? limit = null, int? offset = null,
                                                                     CancellationToken? cancellationToken = null);
}
