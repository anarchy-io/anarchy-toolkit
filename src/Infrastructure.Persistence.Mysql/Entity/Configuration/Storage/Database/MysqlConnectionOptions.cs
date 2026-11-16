using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;

/// <summary>MySQL connection settings. Port defaults to 3306; TimeZone is applied to each opened session.</summary>
/// <param name="HostReader">Host used for reader (read-only replica) connections.</param>
/// <param name="HostWriter">Host used for writer (primary) connections.</param>
/// <param name="Username">Database user name.</param>
/// <param name="Password">Database password.</param>
/// <param name="DbName">Database (schema) name.</param>
/// <param name="TimeZone">
/// Session time zone (for example <c>+00:00</c> or a named zone loaded on the server); the server default when empty.
/// </param>
/// <param name="Port">Database port. Defaults to 3306 when null.</param>
/// <param name="CommandTimeout">Default command timeout in seconds (minimum 1).</param>
public sealed record MysqlConnectionOptions(
    Uri HostReader, Uri HostWriter, string Username, string Password, string DbName,
    string TimeZone, uint? Port, int CommandTimeout)
    : DbConnectionOptions(HostReader, HostWriter, Username, Password, DbName, Port, CommandTimeout);
