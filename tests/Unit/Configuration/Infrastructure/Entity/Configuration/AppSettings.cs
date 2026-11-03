using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Tests.Unit.Configuration.Infrastructure.Entity.Configuration;

/// <summary>
/// Test application settings implementing every capability interface of <see cref="IAppSettings"/> shipped by the
/// toolkit, bound from the <c>appsettings*.json</c> files embedded in this test assembly. Consumers declare their
/// own settings type; the toolkit never depends on a concrete one.
/// </summary>
public record AppSettings :
    IAuthServiceAppSettings,
    IOAuthAppSettings,
    IAwsAppSettings,
    IPostgresAppSettings,
    IResilienceAppSettings,
    IWorkerHealthCheckAppSettings
{
    [Required] public AuthServiceSettings AuthService { get; init; } = null!;
    [Required] public OAuthSettings OAuth { get; init; } = null!;
    [Required] public AwsSettings Aws { get; init; } = null!;
    [Required] public DbConnectionSettings Postgres { get; init; } = null!;
    [Required] public ResilienceSettings Resilience { get; init; } = null!;
    [Required] public WorkerHealthCheckSettings WorkerHealthCheck { get; init; } = null!;
}
