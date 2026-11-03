using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;

/// <summary>
///     Options that describe how to build a Postgres connection string.
///     The <see cref="DbConnectionOptions.HostReader" /> / <see cref="DbConnectionOptions.HostWriter" /> split is
///     selected at runtime based on the <see cref="DataStorageInstanceType" />.
/// </summary>
/// <param name="HostReader">Host used for reader (read-only replica) connections.</param>
/// <param name="HostWriter">Host used for writer (primary) connections.</param>
/// <param name="Username">Database user name.</param>
/// <param name="Password">Database password.</param>
/// <param name="DbName">Database (catalog) name.</param>
/// <param name="TimeZone">Session time zone.</param>
/// <param name="Port">Database port. Defaults to 5432 when null.</param>
/// <param name="CommandTimeout">Command timeout in the unit expected by Npgsql (seconds).</param>
public sealed record PostgresConnectionOptions(
    Uri HostReader,
    Uri HostWriter,
    string Username,
    string Password,
    string DbName,
    string TimeZone,
    uint? Port,
    int CommandTimeout)
    : DbConnectionOptions(HostReader, HostWriter, Username, Password, DbName, Port, CommandTimeout);
