using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;

/// <summary>
///     Envelope for the connection being shared across UoW and policies
/// </summary>
/// <remarks>
///     Stored in an <see cref="Service.Storage.IAsyncFlowContext{TValue}"/> by the <see cref="Service.Storage.IUnitOfWork"/>
///     for the duration of a session and handed to <see cref="Service.Storage.StoragePolicyCallback{TStorage, TResult}"/>
///     callbacks.
/// </remarks>
/// <typeparam name="T">The type of data storage connection the envelope wraps</typeparam>
/// <param name="DataStorageConnection">The wrapped storage connection.</param>
/// <param name="InstanceType">Whether the connection targets a writer or a reader instance.</param>
/// <param name="ConnectionId">An identifier of the connection, for observability.</param>
/// <param name="StartDate">When the connection was acquired.</param>
/// <param name="IsInTransaction">Whether a transaction is currently active on the connection.</param>
public abstract record StorageConnectionEnvelopeBase<T>(
    T DataStorageConnection,
    DataStorageInstanceType InstanceType,
    string ConnectionId,
    DateTimeOffset StartDate,
    bool IsInTransaction = false)
{
    /// <summary>
    ///     Gets or sets whether a transaction is currently active on the connection. Updated by the unit of work when a
    ///     transaction begins, commits or rolls back.
    /// </summary>
    public bool IsInTransaction { get; set; } = IsInTransaction;
}
