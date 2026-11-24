using Anarchy.Toolkit.Core.Persistence.Abstractions.Model;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Core.ValueSet.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event;

/// <summary>
/// Resolves event type identifiers using the caller's backing database.
/// </summary>
/// <remarks>
/// The repository's callbacks do not expose a database identity or a transaction's commit outcome. Consequently,
/// neither hydrated rows nor newly inserted identifiers can safely be shared between calls. Every lookup goes through
/// the callbacks instead; the database is responsible for coordinating concurrent first-time inserts. This also keeps
/// rolled-back identifiers from being reused by another transaction or database.
/// </remarks>
public class EventWriterMultiAttributeIdCache : AttributeIdCacheBase<EventTypeTable, int>
{
    /// <summary>Resolves an identifier without publishing transaction-local values to a shared cache.</summary>
    /// <param name="typeAttribute">The type table to query.</param>
    /// <param name="attributeKey">The type name to resolve.</param>
    /// <param name="callbackHydrateAll">Loads rows visible to the caller's database transaction.</param>
    /// <param name="callbackInsertMissing">Gets or inserts the missing row in the caller's database.</param>
    /// <param name="cancellationToken">Token forwarded to the callbacks.</param>
    /// <returns>The identifier visible to the caller.</returns>
    /// <remarks>
    /// The insert callback must handle concurrent registration of the same name using database constraints and the
    /// provider's get-or-insert semantics. No process-wide locks are held while a transaction is open.
    /// </remarks>
    public new static Task<int> GetOrAddAsync(
        EventTypeTable typeAttribute,
        string attributeKey,
        HydrateAllAsync callbackHydrateAll,
        InsertMissingAsync callbackInsertMissing,
        CancellationToken? cancellationToken = null) =>
        AttributeIdResolver<EventTypeTable, int>.GetOrAddAsync(
            typeAttribute, attributeKey, callbackHydrateAll, callbackInsertMissing, cancellationToken);
}
