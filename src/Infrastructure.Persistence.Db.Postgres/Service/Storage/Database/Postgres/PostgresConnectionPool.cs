using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Entity.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Storage.Database.Postgres;

/// <summary>
/// PostgreSQL connection pool for one instance type (reader or writer), built on
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbConnectionPoolBase{TDbConnection}"/>.
/// </summary>
/// <remarks>
/// Session settings such as the time zone are part of the Npgsql connection string, so no per-session setup is needed.
/// Registered as a keyed singleton (<c>"writer"</c>/<c>"reader"</c>) by <c>ModulePostgres</c>.
/// </remarks>
/// <param name="instanceType">The instance type the pool connects to.</param>
/// <param name="connectionString">Builds the connection string for <paramref name="instanceType"/>.</param>
public class PostgresConnectionPool(DataStorageInstanceType instanceType, PostgresConnectionString connectionString)
    : DbConnectionPoolBase<NpgsqlConnection>(instanceType, connectionString.Value(instanceType))
{
    /// <inheritdoc/>
    protected override NpgsqlConnection CreateConnection(string connectionString)
    {
        return new NpgsqlConnection(connectionString);
    }

    /// <inheritdoc/>
    /// <returns>A new <see cref="PostgresConnectionEnvelope"/>.</returns>
    protected override StorageConnectionEnvelopeBase<NpgsqlConnection> WrapConnection(NpgsqlConnection connection)
    {
        return new PostgresConnectionEnvelope(connection, InstanceType);
    }
}
