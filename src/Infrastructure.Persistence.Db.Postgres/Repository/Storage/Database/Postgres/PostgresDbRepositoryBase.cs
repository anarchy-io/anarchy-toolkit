using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Storage.Database.Postgres;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Repository.Storage.Database.Postgres;

/// <summary>
/// PostgreSQL flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.DbStorageRepositoryBase{TDbConnection, TDialect}"/>,
/// binding the connection type to <see cref="NpgsqlConnection"/> and the dialect to <see cref="PostgresDbDialect"/>.
/// </summary>
/// <remarks>
/// Derive from it for custom PostgreSQL repositories that need embedded SQL loading, sequence lookups and savepoints but none
/// of the reader/writer contracts.
/// </remarks>
/// <param name="dbPolicy">Storage policy used to execute database operations (the writer policy when resolved unkeyed).</param>
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
public abstract class PostgresDbRepositoryBase(IStoragePolicy<NpgsqlConnection> dbPolicy, IAppTelemetry telemetry)
    : DbStorageRepositoryBase<NpgsqlConnection, PostgresDbDialect>(dbPolicy, telemetry);
