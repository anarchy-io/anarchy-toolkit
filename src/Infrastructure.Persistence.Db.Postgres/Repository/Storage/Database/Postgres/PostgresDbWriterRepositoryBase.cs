using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Storage.Database.Postgres;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Repository.Storage.Database.Postgres;

/// <summary>
/// PostgreSQL flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.DbWriterRepositoryBase{TDbConnection, TDialect, TNewEntity, TPersistedEntity}"/>
/// for single-entity write repositories backed by embedded SQL files.
/// </summary>
/// <typeparam name="TNewEntity">The entity type accepted for saving.</typeparam>
/// <typeparam name="TPersistedEntity">The persisted entity type returned after saving.</typeparam>
/// <param name="dbPolicy">Storage policy used to execute database operations (the writer policy when resolved unkeyed).</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
public abstract class PostgresDbWriterRepositoryBase<TNewEntity, TPersistedEntity>(
    IStoragePolicy<NpgsqlConnection> dbPolicy,
    AppTelemetry telemetry)
    : DbWriterRepositoryBase<NpgsqlConnection, PostgresDbDialect, TNewEntity, TPersistedEntity>(dbPolicy,
        telemetry)
    where TNewEntity : IEntity
    where TPersistedEntity : IPersistedEntity;
