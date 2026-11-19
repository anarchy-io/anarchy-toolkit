using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Repository.Storage.Database.Mysql;

/// <summary>
/// MySQL flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.DbWriterRepositoryBase{TDbConnection, TDialect, TNewEntity, TPersistedEntity}"/>
/// for single-entity write repositories backed by embedded SQL files.
/// </summary>
/// <typeparam name="TNewEntity">The entity type accepted for saving.</typeparam>
/// <typeparam name="TPersistedEntity">The persisted entity type returned after saving.</typeparam>
/// <param name="dbPolicy">Storage policy used to execute database operations (the writer policy when resolved unkeyed).</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
public abstract class MysqlDbWriterRepositoryBase<TNewEntity, TPersistedEntity>(IStoragePolicy<MySqlConnection> dbPolicy, IAppTelemetry telemetry)
    : DbWriterRepositoryBase<MySqlConnection, MysqlDbDialect, TNewEntity, TPersistedEntity>(dbPolicy, telemetry)
    where TNewEntity : IEntity where TPersistedEntity : IPersistedEntity;
