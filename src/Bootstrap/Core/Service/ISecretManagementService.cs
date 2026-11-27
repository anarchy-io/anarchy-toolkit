namespace Anarchy.Toolkit.Bootstrap.Core.Service;

/// <summary>
/// Decrypts encrypted configuration values (secrets).
/// </summary>
/// <remarks>
/// <see cref="Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration.ModuleLibConfiguration{TAppSettingsDiModule, TAppSettings}"/>
/// registers <see cref="Anarchy.Toolkit.Bootstrap.Service.AppSecret.InMemoryAppSecretService"/> (pass-through) when the
/// environment is local and no remote implementation; opt into one with its <c>WithSecretManagement</c> method (for
/// example AWS KMS through <c>ModuleAwsSecretManagement</c> in <c>Anarchy.Toolkit.Bootstrap.Aws</c>).
/// </remarks>
public interface ISecretManagementService
{
    /// <summary>
    /// Decrypts an encrypted string value.
    /// </summary>
    /// <param name="value">The encrypted value (format depends on the implementation).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The decrypted plain-text value.</returns>
    Task<string> DecryptStringAsync(string value, CancellationToken? cancellationToken = null);
}

/// <summary>
/// Marker for an <see cref="ISecretManagementService"/> that caches decrypted values, e.g.
/// <see cref="Anarchy.Toolkit.Bootstrap.Service.AppSecret.CachedSecretManagementService"/>.
/// </summary>
public interface ICachedSecretManagementService : ISecretManagementService
{
}
