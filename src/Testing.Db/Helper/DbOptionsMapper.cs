using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Testing.Db.Helper;

/// <summary>
///     Maps the application settings sections (bound from configuration) onto the provider-agnostic database
///     options records.
/// </summary>
public static class DbOptionsMapper
{
    /// <summary>
    ///     Default command timeout (seconds) applied, as the settings do not carry one.
    /// </summary>
    public const int DefaultCommandTimeoutSeconds = 30;

    /// <summary>
    ///     Build <see cref="DbConnectionOptions" /> from a <see cref="DbConnectionSettings" /> section.
    /// </summary>
    public static DbConnectionOptions ConnectionOptions(DbConnectionSettings settings)
    {
        return new DbConnectionOptions(
            settings.HostReader,
            settings.HostWriter,
            settings.Username,
            settings.Password,
            settings.DbName,
            settings.Port,
            DefaultCommandTimeoutSeconds);
    }

    /// <summary>
    ///     Build <see cref="DbResilienceOptions" /> from <see cref="IResilienceAppSettings.Resilience" />.
    /// </summary>
    public static DbResilienceOptions ResilienceOptions(IResilienceAppSettings appSettings)
    {
        return new DbResilienceOptions(appSettings.Resilience.RetryCount);
    }
}
