using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;

public record AuthServiceSettings
{
    [Required] public Uri Host { get; init; } = default!;
    [Required] public string ClientId { get; init; } = default!;
    [Required] public string ClientSecretName { get; init; } = default!;
}

public interface IAuthServiceAppSettings : IAppSettings
{
    [Required] public AuthServiceSettings AuthService { get; init; }
}
