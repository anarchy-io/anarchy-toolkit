using Anarchy.Toolkit.Core.Events.Entity.Event;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.ValueSet.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Repository.Storage.Event;

public interface IEventEnvelopeWriterRepository<in TEventEnvelope> : IStorageBatchWriterRepository<TEventEnvelope>
    where TEventEnvelope : IEventEnvelope
{
    Task<int> GetIdForTypeAsync(EventTypeTable typeTable, string typeName,
                                CancellationToken? cancellationToken = null);
}
