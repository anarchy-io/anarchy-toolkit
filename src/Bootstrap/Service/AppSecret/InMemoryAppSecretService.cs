using Anarchy.Toolkit.Bootstrap.Core.Service;

namespace Anarchy.Toolkit.Bootstrap.Service.AppSecret;

/// <summary>
/// Pass-through <see cref="ISecretManagementService"/> used in local environments: values are returned unchanged.
/// </summary>
public sealed class InMemoryAppSecretService : ISecretManagementService
{
    /// <summary>
    /// Returns <paramref name="value"/> unchanged.
    /// </summary>
    /// <param name="value">The (plain-text) value.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>A completed task with <paramref name="value"/>.</returns>
    public Task<string> DecryptStringAsync(string value, CancellationToken? cancellationToken = null)
    {
        return Task.FromResult(value);
    }
}
