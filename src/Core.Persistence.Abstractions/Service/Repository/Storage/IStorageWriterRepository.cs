using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;

public interface IStorageWriterRepository<in TNewEntity, TPersistedEntity> : IStorageRepository
    where TNewEntity : IEntity
    where TPersistedEntity : IPersistedEntity
{
    Task<TPersistedEntity> SaveAsync(TNewEntity entity, CancellationToken? cancellationToken = null);
}
