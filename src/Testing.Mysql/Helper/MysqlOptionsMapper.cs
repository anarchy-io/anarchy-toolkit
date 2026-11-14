using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Testing.Db.Helper;
using Anarchy.Toolkit.Testing.Mysql.Entity.Configuration;

namespace Anarchy.Toolkit.Testing.Mysql.Helper;

public static class MysqlOptionsMapper
{
    public const int DefaultCommandTimeoutSeconds = DbOptionsMapper.DefaultCommandTimeoutSeconds;

    public static MysqlConnectionOptions ConnectionOptions(IMysqlAppSettings appSettings)
    {
        var settings = appSettings.Mysql;
        return new MysqlConnectionOptions(settings.HostReader, settings.HostWriter, settings.Username, settings.Password,
            settings.DbName, settings.TimeZone, settings.Port, DefaultCommandTimeoutSeconds);
    }

    public static MysqlResilienceOptions ResilienceOptions(IResilienceAppSettings appSettings) =>
        new(appSettings.Resilience.RetryCount);
}
