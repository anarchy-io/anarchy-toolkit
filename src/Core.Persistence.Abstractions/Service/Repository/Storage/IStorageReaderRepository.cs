using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;

/// <summary>
///     Repository that reads persisted entities.
/// </summary>
/// <typeparam name="TPersistedEntity">The persisted entity type.</typeparam>
public interface IStorageReaderRepository<TPersistedEntity> : IStorageRepository
    where TPersistedEntity : IPersistedEntity
{
    /// <summary>
    ///     Gets the entity with the given identifier.
    /// </summary>
    /// <param name="id">The storage identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching entity.</returns>
    /// <exception cref="Exceptions.EntityNotFoundException">Implementations throw when no entity matches.</exception>
    Task<TPersistedEntity> GetByIdAsync(long id, CancellationToken? cancellationToken = null);

    /// <summary>
    ///     Gets a page of entities.
    /// </summary>
    /// <param name="limit">Maximum number of entities to return; <see langword="null"/> for no limit.</param>
    /// <param name="offset">Number of entities to skip; <see langword="null"/> to start at the beginning.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The entities found.</returns>
    Task<IReadOnlyCollection<TPersistedEntity>> GetAllAsync(int? limit = null, int? offset = null,
                                                            CancellationToken? cancellationToken = null);
}
