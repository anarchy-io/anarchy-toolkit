using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;

/// <summary>
///     Repository that stores new entities and returns their persisted representation.
/// </summary>
/// <typeparam name="TNewEntity">The type of the (not yet persisted) entity to store.</typeparam>
/// <typeparam name="TPersistedEntity">The persisted representation, carrying the storage identity.</typeparam>
public interface IStorageWriterRepository<in TNewEntity, TPersistedEntity> : IStorageRepository
    where TNewEntity : IEntity
    where TPersistedEntity : IPersistedEntity
{
    /// <summary>
    ///     Stores <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">The entity to store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted entity, including its storage-assigned <see cref="IPersistedEntity.Id"/>.</returns>
    Task<TPersistedEntity> SaveAsync(TNewEntity entity, CancellationToken? cancellationToken = null);
}
