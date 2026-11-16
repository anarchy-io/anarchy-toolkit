using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;

/// <summary>
/// Settings of an OAuth client using user-name/password credentials stored as secrets. All properties are required.
/// </summary>
public record OAuthSettings
{
    /// <summary>The base address of the OAuth server.</summary>
    [Required] public Uri Host { get; init; } = default!;

    /// <summary>The name of the secret holding the client user name.</summary>
    [Required] public string ClientUserNameSecretName { get; init; } = default!;

    /// <summary>The name of the secret holding the client password.</summary>
    [Required] public string ClientPasswordSecretName { get; init; } = default!;
}

/// <summary>
/// Capability interface for application settings that contain an <c>OAuth</c> section.
/// </summary>
public interface IOAuthAppSettings : IAppSettings
{
    /// <summary>The OAuth settings (configuration section <c>OAuth</c>). Required.</summary>
    [Required] public OAuthSettings OAuth { get; init; }
}
