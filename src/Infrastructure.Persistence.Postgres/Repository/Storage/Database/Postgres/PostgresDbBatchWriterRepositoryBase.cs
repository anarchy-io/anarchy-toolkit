using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Repository.Storage.Database.Postgres;

/// <summary>
/// PostgreSQL flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.DbBatchWriterRepositoryBase{TDbConnection, TDialect, TNewEntity}"/>
/// for repositories that save collections of entities.
/// </summary>
/// <typeparam name="TNewEntity">The entity type accepted for saving.</typeparam>
/// <param name="dbPolicy">Storage policy used to execute database operations (the writer policy when resolved unkeyed).</param>
/// <param name="observer">Observer used for logging and metrics.</param>
public abstract class PostgresDbBatchWriterRepositoryBase<TNewEntity>(
    IStoragePolicy<NpgsqlConnection> dbPolicy,
    Observer observer)
    : DbBatchWriterRepositoryBase<NpgsqlConnection, PostgresDbDialect, TNewEntity>(dbPolicy, observer)
    where TNewEntity : IEntity;
