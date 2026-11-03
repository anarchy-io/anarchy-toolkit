using Anarchy.Toolkit.Core.Events.Entity.Event.InMemory;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.LocalEvent.Service.Storage.UnitOfWork;

public record UowTransactionLocalEvent : LocalEventEnvelope
{
    public required DataStorageInstanceType ConnectionInstanceType;
    public required string ConnectionId;
    public required DateTimeOffset ConnectionStartDate;

    public static UowTransactionLocalEvent FromConnectionEnvelope<TDbConnection>(
        StorageConnectionEnvelopeBase<TDbConnection> connectionEnvelope, string eventSource)
        where TDbConnection : DbConnection
    {
        return new UowTransactionLocalEvent {
            EventSource = eventSource,
            ConnectionInstanceType = connectionEnvelope.InstanceType,
            ConnectionId = connectionEnvelope.ConnectionId,
            ConnectionStartDate = connectionEnvelope.StartDate
        };
    }
}
