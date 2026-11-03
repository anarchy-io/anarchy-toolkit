using Anarchy.Toolkit.Configuration.Core.Service;

namespace Anarchy.Toolkit.Configuration.Service.AppSecret;

public sealed class InMemoryAppSecretService : ISecretManagementService
{
    public Task<string> DecryptStringAsync(string value, CancellationToken? cancellationToken = null)
    {
        return Task.FromResult(value);
    }
}
