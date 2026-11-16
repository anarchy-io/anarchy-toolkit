using Anarchy.Toolkit.Configuration.Core.Service;
using Microsoft.Extensions.Caching.Memory;

namespace Anarchy.Toolkit.Configuration.Service.AppSecret;

/// <summary>
/// Decorator that caches the results of another <see cref="ISecretManagementService"/> in an
/// <see cref="IMemoryCache"/>, keyed by the encrypted value, without expiration.
/// </summary>
/// <param name="secretManagement">The underlying service performing the decryption.</param>
/// <param name="memoryCache">The cache storing decrypted values.</param>
public class CachedSecretManagementService(
    ISecretManagementService secretManagement,
    IMemoryCache memoryCache)
    : ICachedSecretManagementService
{
    /// <summary>
    /// Returns the cached decrypted value for <paramref name="value"/>, decrypting and caching it on first use.
    /// </summary>
    /// <param name="value">The encrypted value; also used as the cache key.</param>
    /// <param name="cancellationToken">Optional cancellation token passed to the underlying service.</param>
    /// <returns>The decrypted plain-text value.</returns>
    public Task<string> DecryptStringAsync(string value, CancellationToken? cancellationToken = null)
    {
        return memoryCache.GetOrCreateAsync(value,
            async _ => await secretManagement.DecryptStringAsync(value, cancellationToken))!;
    }
}
