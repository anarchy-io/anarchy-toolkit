using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;

public interface IStorageReaderRepository<TPersistedEntity> : IStorageRepository
    where TPersistedEntity : IPersistedEntity
{
    Task<TPersistedEntity> GetByIdAsync(long id, CancellationToken? cancellationToken = null);
    Task<IReadOnlyCollection<TPersistedEntity>> GetAllAsync(int? limit = null, int? offset = null,
                                                            CancellationToken? cancellationToken = null);
}
