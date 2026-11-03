using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;

/// <summary>
///     Envelope for the connection being shared across UoW and policies
/// </summary>
/// <typeparam name="T">The type of data storage connection the envelope wraps</typeparam>
public abstract record StorageConnectionEnvelopeBase<T>(
    T DataStorageConnection,
    DataStorageInstanceType InstanceType,
    string ConnectionId,
    DateTimeOffset StartDate,
    bool IsInTransaction = false)
{
    public bool IsInTransaction { get; set; } = IsInTransaction;
}
