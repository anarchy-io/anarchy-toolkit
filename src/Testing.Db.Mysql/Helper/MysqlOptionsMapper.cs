using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Testing.Db.Helper;
using Anarchy.Toolkit.Testing.Db.Mysql.Entity.Configuration;

namespace Anarchy.Toolkit.Testing.Db.Mysql.Helper;

/// <summary>
///     Maps the application settings sections (bound from configuration) onto the strongly typed MySQL
///     options records consumed by the persistence concretes.
/// </summary>
public static class MysqlOptionsMapper
{
    /// <summary>
    ///     Default command timeout (seconds) applied, as the settings do not carry one.
    /// </summary>
    public const int DefaultCommandTimeoutSeconds = DbOptionsMapper.DefaultCommandTimeoutSeconds;

    /// <summary>
    ///     Build <see cref="MysqlConnectionOptions" /> from <see cref="IMysqlAppSettings.Mysql" />.
    /// </summary>
    /// <param name="appSettings">Settings exposing the <c>Mysql</c> section.</param>
    /// <returns>The connection options, using <see cref="DefaultCommandTimeoutSeconds" /> as command timeout.</returns>
    public static MysqlConnectionOptions ConnectionOptions(IMysqlAppSettings appSettings)
    {
        var settings = appSettings.Mysql;
        return new MysqlConnectionOptions(settings.HostReader, settings.HostWriter, settings.Username, settings.Password,
            settings.DbName, settings.TimeZone, settings.Port, DefaultCommandTimeoutSeconds);
    }

    /// <summary>
    ///     Build <see cref="MysqlResilienceOptions" /> from <see cref="IResilienceAppSettings.Resilience" />.
    /// </summary>
    /// <param name="appSettings">Settings exposing the <c>Resilience</c> section.</param>
    /// <returns>The resilience options, carrying the configured retry count.</returns>
    public static MysqlResilienceOptions ResilienceOptions(IResilienceAppSettings appSettings) =>
        new(appSettings.Resilience.RetryCount);
}
