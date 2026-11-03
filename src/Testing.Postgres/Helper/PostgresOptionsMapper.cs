using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Testing.Db.Helper;

namespace Anarchy.Toolkit.Testing.Postgres.Helper;

/// <summary>
///     Maps the application settings sections (bound from configuration) onto the strongly typed Postgres
///     options records consumed by the persistence concretes.
/// </summary>
public static class PostgresOptionsMapper
{
    /// <summary>
    ///     Default command timeout (seconds) applied, as the settings do not carry one.
    /// </summary>
    public const int DefaultCommandTimeoutSeconds = DbOptionsMapper.DefaultCommandTimeoutSeconds;

    /// <summary>
    ///     Build <see cref="PostgresConnectionOptions" /> from <see cref="IPostgresAppSettings.Postgres" />.
    /// </summary>
    public static PostgresConnectionOptions ConnectionOptions(IPostgresAppSettings appSettings)
    {
        var settings = appSettings.Postgres;

        return new PostgresConnectionOptions(
            settings.HostReader,
            settings.HostWriter,
            settings.Username,
            settings.Password,
            settings.DbName,
            settings.TimeZone,
            settings.Port,
            DefaultCommandTimeoutSeconds);
    }

    /// <summary>
    ///     Build <see cref="PostgresResilienceOptions" /> from <see cref="IResilienceAppSettings.Resilience" />.
    /// </summary>
    public static PostgresResilienceOptions ResilienceOptions(IResilienceAppSettings appSettings)
    {
        return new PostgresResilienceOptions(appSettings.Resilience.RetryCount);
    }
}
