using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Storage.Database;

public sealed record PostgresConnectionEnvelope : DbConnectionEnvelope<NpgsqlConnection>
{
    public PostgresConnectionEnvelope(NpgsqlConnection npgsql, DataStorageInstanceType instanceType) : base(npgsql,
        instanceType)
    {
    }
}
