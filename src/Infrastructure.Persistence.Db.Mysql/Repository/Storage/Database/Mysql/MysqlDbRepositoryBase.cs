using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Repository.Storage.Database.Mysql;

/// <summary>
/// MySQL flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.DbStorageRepositoryBase{TDbConnection, TDialect}"/>,
/// binding the connection type to <see cref="MySqlConnection"/> and the dialect to <see cref="MysqlDbDialect"/>.
/// </summary>
/// <remarks>
/// Derive from it for custom MySQL repositories that need embedded SQL loading, sequence lookups and savepoints but none
/// of the reader/writer contracts.
/// </remarks>
/// <param name="dbPolicy">Storage policy used to execute database operations (the writer policy when resolved unkeyed).</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
public abstract class MysqlDbRepositoryBase(IStoragePolicy<MySqlConnection> dbPolicy, AppTelemetry telemetry)
    : DbStorageRepositoryBase<MySqlConnection, MysqlDbDialect>(dbPolicy, telemetry);
