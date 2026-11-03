using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;

public interface IStorageBatchWriterRepository<in TNewEntity>
    where TNewEntity : IEntity
{
    Task<int> SaveAllAsync(IReadOnlyCollection<TNewEntity> entities, CancellationToken? cancellationToken = null);
}
