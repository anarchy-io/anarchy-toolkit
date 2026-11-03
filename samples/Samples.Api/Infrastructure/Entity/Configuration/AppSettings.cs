using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Samples.Api.Infrastructure.Entity.Configuration;

public sealed class AppSettings : IAuthServiceAppSettings
{
    [Required] public string Greeting { get; init; } = default!;

    [Required] public AuthServiceSettings AuthService { get; init; } = default!;
}
