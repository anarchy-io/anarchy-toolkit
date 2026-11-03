using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Storage.Database;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;

public class PostgresConnectionPool(DataStorageInstanceType instanceType, PostgresConnectionString connectionString)
    : DbConnectionPoolBase<NpgsqlConnection>(instanceType, connectionString.Value(instanceType))
{
    protected override NpgsqlConnection CreateConnection(string connectionString)
    {
        return new NpgsqlConnection(connectionString);
    }

    protected override StorageConnectionEnvelopeBase<NpgsqlConnection> WrapConnection(NpgsqlConnection connection)
    {
        return new PostgresConnectionEnvelope(connection, InstanceType);
    }
}
