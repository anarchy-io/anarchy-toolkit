using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;

/// <summary>
///     Repository capable of storing many new entities in a single batch operation.
/// </summary>
/// <typeparam name="TNewEntity">The type of the (not yet persisted) entities to store.</typeparam>
public interface IStorageBatchWriterRepository<in TNewEntity>
    where TNewEntity : IEntity
{
    /// <summary>
    ///     Stores all <paramref name="entities"/> in one batch.
    /// </summary>
    /// <param name="entities">The entities to store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The number of stored entities (rows affected).</returns>
    /// <exception cref="Exceptions.EmptyEntityListForBatchSaveStorageException">
    ///     Implementations may throw when <paramref name="entities"/> is empty.
    /// </exception>
    Task<int> SaveAllAsync(IReadOnlyCollection<TNewEntity> entities, CancellationToken? cancellationToken = null);
}
