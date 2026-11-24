using Anarchy.Toolkit.Core.Events.Local;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.LocalEvent.Service.Storage.UnitOfWork;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Service.Storage.Database.Event.LocalEventHandlers;

/// <summary>
/// Clears inherited attribute-cache state when a storage transaction is rolled back.
/// </summary>
/// <remarks>
/// <see cref="EventWriterMultiAttributeIdCache"/> no longer shares identifiers between lookups, so registering this
/// handler is not required for event type resolution. It remains available for code using the inherited cache API.
/// </remarks>
public sealed class RolledBackTransactionEventHandler : ILocalEventHandler<RolledBackUowTransactionLocalEvent>
{
    /// <summary>Clears <see cref="EventWriterMultiAttributeIdCache"/>.</summary>
    /// <param name="event">The rolled-back transaction event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation (unused).</param>
    /// <returns>A completed task.</returns>
    public Task HandleAsync(RolledBackUowTransactionLocalEvent @event,
                            CancellationToken? cancellationToken = null)
    {
        // EventWriterMultiAttributeIdCache resolves from the caller's database on every lookup; no transaction-local
        // identifier is published to shared state.
        EventWriterMultiAttributeIdCache.Clear();

        return Task.CompletedTask;
    }
}
