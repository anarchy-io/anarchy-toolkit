using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.LocalEvent.Service.Storage.UnitOfWork;

/// <summary>
/// Published after a storage transaction has been rolled back.
/// </summary>
/// <remarks>
/// Handled by <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Event.LocalEventHandlers.RolledBackTransactionEventHandler"/>
/// to invalidate cached type identifiers that may no longer exist in the database.
/// </remarks>
public sealed record RolledBackUowTransactionLocalEvent : UowTransactionLocalEvent
{
    /// <summary>The exception that caused the rollback, or <see langword="null"/> when none was supplied.</summary>
    public Exception? Reason;

    /// <summary>Creates a rolled-back-transaction event describing the given connection envelope.</summary>
    /// <typeparam name="TDbConnection">Type of the database connection.</typeparam>
    /// <param name="connectionEnvelope">The connection envelope the transaction ran on.</param>
    /// <param name="eventSource">Name of the component publishing the event.</param>
    /// <param name="reason">The exception that caused the rollback, if any.</param>
    /// <returns>A new event populated from <paramref name="connectionEnvelope"/>.</returns>
    public static RolledBackUowTransactionLocalEvent FromConnectionEnvelope<TDbConnection>(
        StorageConnectionEnvelopeBase<TDbConnection> connectionEnvelope, string? eventSource = null, Exception? reason = null)
        where TDbConnection : DbConnection
    {
        return new RolledBackUowTransactionLocalEvent {
            EventSource = eventSource,
            ConnectionInstanceType = connectionEnvelope.InstanceType,
            ConnectionId = connectionEnvelope.ConnectionId,
            ConnectionStartDate = connectionEnvelope.StartDate,
            Reason = reason
        };
    }
}
