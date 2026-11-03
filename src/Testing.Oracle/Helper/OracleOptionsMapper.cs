using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Testing.Db.Helper;

namespace Anarchy.Toolkit.Testing.Oracle.Helper;

/// <summary>
///     Maps the application settings sections (bound from configuration) onto the strongly typed Oracle
///     options records consumed by the persistence concretes.
/// </summary>
public static class OracleOptionsMapper
{
    /// <summary>
    ///     Default command timeout (seconds) applied, as the settings do not carry one.
    /// </summary>
    public const int DefaultCommandTimeoutSeconds = DbOptionsMapper.DefaultCommandTimeoutSeconds;

    /// <summary>
    ///     Build <see cref="OracleConnectionOptions" /> from <see cref="IOracleAppSettings.Oracle" />.
    /// </summary>
    public static OracleConnectionOptions ConnectionOptions(IOracleAppSettings appSettings)
    {
        var settings = appSettings.Oracle;

        return new OracleConnectionOptions(
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
    ///     Build <see cref="OracleResilienceOptions" /> from <see cref="IResilienceAppSettings.Resilience" />.
    /// </summary>
    public static OracleResilienceOptions ResilienceOptions(IResilienceAppSettings appSettings)
    {
        return new OracleResilienceOptions(appSettings.Resilience.RetryCount);
    }
}
