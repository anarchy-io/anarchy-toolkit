using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;

/// <summary>
/// Settings of an authentication service client. All properties are required.
/// </summary>
public record AuthServiceSettings
{
    /// <summary>The base address of the authentication service.</summary>
    [Required] public Uri Host { get; init; } = default!;

    /// <summary>The client id.</summary>
    [Required] public string ClientId { get; init; } = default!;

    /// <summary>The name of the secret holding the client secret (not the secret value itself).</summary>
    [Required] public string ClientSecretName { get; init; } = default!;
}

/// <summary>
/// Capability interface for application settings that contain an <c>AuthService</c> section.
/// </summary>
public interface IAuthServiceAppSettings : IAppSettings
{
    /// <summary>The authentication service settings (configuration section <c>AuthService</c>). Required.</summary>
    [Required] public AuthServiceSettings AuthService { get; init; }
}
