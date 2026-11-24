using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Core.ValueSet.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Core.Service.Repository.Storage.Event;

/// <summary>
/// Batch writer for event envelopes that also resolves the numeric identifiers of
/// the auxiliary type tables (aggregate and event types) referenced by the stored rows.
/// </summary>
/// <typeparam name="TEventEnvelope">Type of event envelope persisted by the repository.</typeparam>
/// <remarks>
/// The provider-agnostic implementation is
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Events.Repository.Storage.Database.Event.DbEventWriterRepositoryBase{TDbConnection, TDialect, TEventEnvelope}"/>,
/// which resolves type identifiers against the caller's database and transaction.
/// </remarks>
public interface IEventEnvelopeWriterRepository<in TEventEnvelope> : IStorageBatchWriterRepository<TEventEnvelope>
    where TEventEnvelope : IEventEnvelope
{
    /// <summary>
    /// Gets the identifier of <paramref name="typeName"/> in the given type table, inserting the name when it
    /// does not exist yet.
    /// </summary>
    /// <param name="typeTable">The type table to look the name up in.</param>
    /// <param name="typeName">The type name (for example an aggregate or event type name).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The identifier of the type row.</returns>
    Task<int> GetIdForTypeAsync(EventTypeTable typeTable, string typeName,
                                CancellationToken? cancellationToken = null);
}
