using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

/// <summary>
///     Provider-agnostic options that describe how to build a database connection string.
///     The <see cref="HostReader" /> / <see cref="HostWriter" /> split is selected at runtime based on the
///     <see cref="DataStorageInstanceType" />. Providers derive from it to add their own settings.
/// </summary>
/// <param name="HostReader">Host used for reader (read-only replica) connections.</param>
/// <param name="HostWriter">Host used for writer (primary) connections.</param>
/// <param name="Username">Database user name.</param>
/// <param name="Password">Database password.</param>
/// <param name="DbName">Database (catalog / service) name.</param>
/// <param name="Port">Database port. The provider default is used when null.</param>
/// <param name="CommandTimeout">Command timeout in seconds.</param>
public record DbConnectionOptions(
    Uri HostReader,
    Uri HostWriter,
    string Username,
    string Password,
    string DbName,
    uint? Port,
    int CommandTimeout);
