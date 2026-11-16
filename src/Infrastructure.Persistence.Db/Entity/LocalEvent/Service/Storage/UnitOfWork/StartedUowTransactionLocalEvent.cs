using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.LocalEvent.Service.Storage.UnitOfWork;

/// <summary>
/// Published after a storage transaction has been started.
/// </summary>
public sealed record StartedUowTransactionLocalEvent : UowTransactionLocalEvent
{
    /// <summary>Creates a started-transaction event describing the given connection envelope.</summary>
    /// <typeparam name="TDbConnection">Type of the database connection.</typeparam>
    /// <param name="connectionEnvelope">The connection envelope the transaction runs on.</param>
    /// <param name="eventSource">Name of the component publishing the event.</param>
    /// <returns>A new event populated from <paramref name="connectionEnvelope"/>.</returns>
    public new static StartedUowTransactionLocalEvent FromConnectionEnvelope<TDbConnection>(
        StorageConnectionEnvelopeBase<TDbConnection> connectionEnvelope, string? eventSource = null)
        where TDbConnection : DbConnection
    {
        return new StartedUowTransactionLocalEvent {
            EventSource = eventSource,
            ConnectionInstanceType = connectionEnvelope.InstanceType,
            ConnectionId = connectionEnvelope.ConnectionId,
            ConnectionStartDate = connectionEnvelope.StartDate
        };
    }
}
