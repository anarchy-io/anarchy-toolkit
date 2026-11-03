namespace Anarchy.Toolkit.Configuration.Core.Service;

public interface ISecretManagementService
{
    Task<string> DecryptStringAsync(string value, CancellationToken? cancellationToken = null);
}

public interface ICachedSecretManagementService : ISecretManagementService
{
}
