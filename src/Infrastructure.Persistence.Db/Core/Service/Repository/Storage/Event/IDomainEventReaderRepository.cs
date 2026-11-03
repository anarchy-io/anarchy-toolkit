using Anarchy.Toolkit.Core.Events.Entity.Event.Domain;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;

public interface IDomainEventReaderRepository : IStorageReaderRepository<PersistedEvent>
{
    Task<IReadOnlyCollection<PersistedEvent>> GetByAggregateIdAsync(string aggregateId,
                                                                    CancellationToken? cancellationToken = null);
    Task<IReadOnlyCollection<PersistedEvent>> GetByTypeAggregateAsync(string typeAggregateName,
                                                                      CancellationToken? cancellationToken = null);
    Task<IReadOnlyCollection<PersistedEvent>> GetByTypeEventAsync(string typeEventName,
                                                                  CancellationToken? cancellationToken = null);
}
