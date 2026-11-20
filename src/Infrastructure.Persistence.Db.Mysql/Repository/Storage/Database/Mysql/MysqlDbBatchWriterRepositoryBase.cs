using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Repository.Storage.Database.Mysql;

/// <summary>
/// MySQL flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.DbBatchWriterRepositoryBase{TDbConnection, TDialect, TNewEntity}"/>
/// for repositories that save collections of entities.
/// </summary>
/// <typeparam name="TNewEntity">The entity type accepted for saving.</typeparam>
/// <param name="dbPolicy">Storage policy used to execute database operations (the writer policy when resolved unkeyed).</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
public abstract class MysqlDbBatchWriterRepositoryBase<TNewEntity>(IStoragePolicy<MySqlConnection> dbPolicy, AppTelemetry telemetry)
    : DbBatchWriterRepositoryBase<MySqlConnection, MysqlDbDialect, TNewEntity>(dbPolicy, telemetry)
    where TNewEntity : IEntity;
