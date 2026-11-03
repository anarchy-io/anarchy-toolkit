using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;

/// <summary>
///     Options that describe how to build an Oracle connection string.
///     The <see cref="DbConnectionOptions.HostReader" /> / <see cref="DbConnectionOptions.HostWriter" /> split is
///     selected at runtime based on the <see cref="DataStorageInstanceType" />.
/// </summary>
/// <param name="HostReader">Host used for reader (read-only replica / standby) connections.</param>
/// <param name="HostWriter">Host used for writer (primary) connections.</param>
/// <param name="Username">Database user name.</param>
/// <param name="Password">Database password.</param>
/// <param name="DbName">Service name of the database.</param>
/// <param name="TimeZone">Session time zone (for example <c>Europe/Amsterdam</c>); the client default when empty.</param>
/// <param name="Port">Listener port. Defaults to 1521 when null.</param>
/// <param name="CommandTimeout">Command timeout in seconds, applied to every connection.</param>
public sealed record OracleConnectionOptions(
    Uri HostReader,
    Uri HostWriter,
    string Username,
    string Password,
    string DbName,
    string TimeZone,
    uint? Port,
    int CommandTimeout)
    : DbConnectionOptions(HostReader, HostWriter, Username, Password, DbName, Port, CommandTimeout);
