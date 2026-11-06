using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using System.ComponentModel.DataAnnotations;

namespace Anarchy.Toolkit.Testing.Mysql.Entity.Configuration;

/// <summary>Settings section used by the MySQL test fixtures.</summary>
public interface IMysqlAppSettings : IAppSettings
{
    [Required] public DbConnectionSettings Mysql { get; init; }
}
