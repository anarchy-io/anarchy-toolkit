using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Core.Service.Repository.Storage.Event;

/// <summary>
/// Read-side repository over the persisted domain event store, returning events as
/// <see cref="PersistedDomainEvent"/> instances.
/// </summary>
/// <remarks>
/// The provider-agnostic implementation is
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event.DbDomainEventReaderRepositoryBase{TDbConnection, TDialect}"/>;
/// each database provider closes it over its own connection and <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database.IDbDialect"/> types.
/// </remarks>
public interface IDomainEventReaderRepository : IStorageReaderRepository<PersistedDomainEvent>
{
    /// <summary>Gets every persisted domain event raised by the aggregate with the given identifier.</summary>
    /// <param name="aggregateId">Identifier of the aggregate instance the events belong to.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching events; an empty collection when none exist.</returns>
    Task<IReadOnlyCollection<PersistedDomainEvent>> GetByAggregateIdAsync(string aggregateId,
                                                                    CancellationToken? cancellationToken = null);
    /// <summary>Gets every persisted domain event raised by aggregates of the given type.</summary>
    /// <param name="typeAggregateName">Name of the aggregate type (as stored in the aggregate type table).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching events; an empty collection when none exist.</returns>
    Task<IReadOnlyCollection<PersistedDomainEvent>> GetByTypeAggregateAsync(string typeAggregateName,
                                                                      CancellationToken? cancellationToken = null);
    /// <summary>Gets every persisted domain event of the given event type.</summary>
    /// <param name="typeEventName">Name of the event type (as stored in the event type table).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching events; an empty collection when none exist.</returns>
    Task<IReadOnlyCollection<PersistedDomainEvent>> GetByTypeEventAsync(string typeEventName,
                                                                  CancellationToken? cancellationToken = null);
}
