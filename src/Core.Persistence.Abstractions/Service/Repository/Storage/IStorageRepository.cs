namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage;

/// <summary>
///     Base contract shared by storage repositories.
/// </summary>
public interface IStorageRepository
{
    /// <summary>
    ///     Gets the latest (most recently assigned) identifier in the storage.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The latest identifier.</returns>
    Task<long> GetLatestIdAsync(CancellationToken? cancellationToken = null);
}
