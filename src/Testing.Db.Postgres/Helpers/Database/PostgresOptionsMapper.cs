using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Testing.Db.Helpers.Database;

namespace Anarchy.Toolkit.Testing.Db.Postgres.Helpers.Database;

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
    /// <param name="appSettings">Settings exposing the <c>Postgres</c> section.</param>
    /// <returns>The connection options, using <see cref="DefaultCommandTimeoutSeconds" /> as command timeout.</returns>
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
    /// <param name="appSettings">Settings exposing the <c>Resilience</c> section.</param>
    /// <returns>The resilience options, carrying the configured retry count.</returns>
    public static PostgresResilienceOptions ResilienceOptions(IResilienceAppSettings appSettings)
    {
        return new PostgresResilienceOptions(appSettings.Resilience.RetryCount);
    }
}
