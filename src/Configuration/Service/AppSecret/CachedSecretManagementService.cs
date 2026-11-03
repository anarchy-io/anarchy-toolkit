using Anarchy.Toolkit.Configuration.Core.Service;
using Microsoft.Extensions.Caching.Memory;

namespace Anarchy.Toolkit.Configuration.Service.AppSecret;

public class CachedSecretManagementService(
    ISecretManagementService secretManagement,
    IMemoryCache memoryCache)
    : ICachedSecretManagementService
{
    public Task<string> DecryptStringAsync(string value, CancellationToken? cancellationToken = null)
    {
        return memoryCache.GetOrCreateAsync(value,
            async _ => await secretManagement.DecryptStringAsync(value, cancellationToken))!;
    }
}
