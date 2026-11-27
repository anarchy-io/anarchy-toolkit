using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Storage.Database;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Samples.Api.Infrastructure.Entity.Configuration;

public sealed class AppSettings : IAuthServiceAppSettings, IPostgresAppSettings, IOracleAppSettings, IResilienceAppSettings
{
    [Required] public string Greeting { get; init; } = default!;

    [Required] public AuthServiceSettings AuthService { get; init; } = default!;

    [Required] public DbConnectionSettings Postgres { get; init; } = default!;

    [Required] public DbConnectionSettings Mysql { get; init; } = default!;

    [Required] public DbConnectionSettings Oracle { get; init; } = default!;

    [Required] public ResilienceSettings Resilience { get; init; } = default!;
}
