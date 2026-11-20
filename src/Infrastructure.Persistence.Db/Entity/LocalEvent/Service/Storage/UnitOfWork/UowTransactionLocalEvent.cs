using Anarchy.Toolkit.Core.Events.Local;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.LocalEvent.Service.Storage.UnitOfWork;

/// <summary>
/// Base in-process event describing a unit-of-work transaction lifecycle change on a given database connection.
/// </summary>
/// <remarks>
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbUnitOfWorkBase{TDbConnection, TDbTransaction}"/>
/// publishes the concrete subclasses (<see cref="StartedUowTransactionLocalEvent"/>,
/// <see cref="CommittedUowTransactionLocalEvent"/>, <see cref="RolledBackUowTransactionLocalEvent"/>) on its optional
/// local event bus, with the unit of work type name as <c>EventSource</c>.
/// </remarks>
public record UowTransactionLocalEvent : LocalEventEnvelope
{
    /// <summary>Whether the transaction runs on a reader or a writer connection.</summary>
    public required DataStorageInstanceType ConnectionInstanceType;

    /// <summary>Unique identifier of the connection envelope the transaction runs on.</summary>
    public required string ConnectionId;

    /// <summary>Moment the connection envelope was created.</summary>
    public required DateTimeOffset ConnectionStartDate;

    /// <summary>Creates an event describing the given connection envelope.</summary>
    /// <typeparam name="TDbConnection">Type of the database connection.</typeparam>
    /// <param name="connectionEnvelope">The connection envelope the transaction runs on.</param>
    /// <param name="eventSource">Name of the component publishing the event.</param>
    /// <returns>A new event populated from <paramref name="connectionEnvelope"/>.</returns>
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
