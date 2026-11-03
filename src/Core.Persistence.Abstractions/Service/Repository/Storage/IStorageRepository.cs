namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;

public interface IStorageRepository
{
    Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null);
}
