using Anarchy.Toolkit.Core.Events.Service.Event.InMemory;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.LocalEvent.Service.Storage.UnitOfWork;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event.LocalEventHandlers;

/// <summary>
/// Invalidates the event writer type-id cache when a storage transaction is rolled back.
/// </summary>
public sealed class RolledBackTransactionEventHandler : ILocalEventHandler<RolledBackUowTransactionLocalEvent>
{
    public Task HandleAsync(RolledBackUowTransactionLocalEvent @event,
                            CancellationToken? cancellationToken = null)
    {
        // If a caller of the repository rolls back a transaction that inserted a new type, after a new type
        // id was cached, the cache will now contain an id that does not exist in the database, potentially
        // leading to foreign key violations in future calls.
        // To prevent this, the cache is invalidated when a transaction is rolled back and re-hydrated
        // on the next access.
        EventWriterMultiAttributeIdCache.Clear();

        return Task.CompletedTask;
    }
}
