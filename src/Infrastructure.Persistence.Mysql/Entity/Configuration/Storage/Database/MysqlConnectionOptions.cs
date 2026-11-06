using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;

/// <summary>MySQL connection settings. Port defaults to 3306; TimeZone is applied to each opened session.</summary>
public sealed record MysqlConnectionOptions(
    Uri HostReader, Uri HostWriter, string Username, string Password, string DbName,
    string TimeZone, uint? Port, int CommandTimeout)
    : DbConnectionOptions(HostReader, HostWriter, Username, Password, DbName, Port, CommandTimeout);
