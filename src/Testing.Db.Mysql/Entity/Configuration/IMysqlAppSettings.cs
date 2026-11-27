using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Storage.Database;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Testing.Db.Mysql.Entity.Configuration;

/// <summary>Settings section used by the MySQL test fixtures.</summary>
/// <remarks>
///     Implement it on the test settings type (together with <see cref="IResilienceAppSettings" /> where a policy is
///     needed) and pass it to <see cref="Anarchy.Toolkit.Testing.Db.Mysql.Helpers.Database.MysqlHelper" /> or
///     <see cref="Anarchy.Toolkit.Testing.Db.Mysql.Helpers.Database.SingletonMysqlHelper" />.
/// </remarks>
public interface IMysqlAppSettings : IAppSettings
{
    /// <summary>MySQL connection settings, bound from the <c>Mysql</c> configuration section.</summary>
    [Required] public DbConnectionSettings Mysql { get; init; }
}
