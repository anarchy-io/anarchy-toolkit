using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;

public record OAuthSettings
{
    [Required] public Uri Host { get; init; } = default!;
    [Required] public string ClientUserNameSecretName { get; init; } = default!;
    [Required] public string ClientPasswordSecretName { get; init; } = default!;
}

public interface IOAuthAppSettings : IAppSettings
{
    [Required] public OAuthSettings OAuth { get; init; }
}
