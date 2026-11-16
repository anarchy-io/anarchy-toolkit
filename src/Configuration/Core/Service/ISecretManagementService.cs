namespace Anarchy.Toolkit.Configuration.Core.Service;

/// <summary>
/// Decrypts encrypted configuration values (secrets).
/// </summary>
/// <remarks>
/// Registered by <c>ModuleSecretManagement</c> (part of
/// <see cref="Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.ModuleLibConfiguration{TAppSettingsDiModule, TAppSettings}"/>):
/// <see cref="Anarchy.Toolkit.Configuration.Service.AppSecret.InMemoryAppSecretService"/> (pass-through) when the
/// environment is local, otherwise <see cref="Anarchy.Toolkit.Configuration.Service.AppSecret.KmsAppSecretService"/>.
/// </remarks>
public interface ISecretManagementService
{
    /// <summary>
    /// Decrypts an encrypted string value.
    /// </summary>
    /// <param name="value">The encrypted value (format depends on the implementation, e.g. Base64 KMS ciphertext).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The decrypted plain-text value.</returns>
    Task<string> DecryptStringAsync(string value, CancellationToken? cancellationToken = null);
}

/// <summary>
/// Marker for an <see cref="ISecretManagementService"/> that caches decrypted values, e.g.
/// <see cref="Anarchy.Toolkit.Configuration.Service.AppSecret.CachedSecretManagementService"/>.
/// </summary>
public interface ICachedSecretManagementService : ISecretManagementService
{
}
