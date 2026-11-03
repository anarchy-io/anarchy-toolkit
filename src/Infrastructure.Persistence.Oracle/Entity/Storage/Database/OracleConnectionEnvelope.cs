using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Storage.Database;

public sealed record OracleConnectionEnvelope : DbConnectionEnvelope<OracleConnection>
{
    public OracleConnectionEnvelope(OracleConnection connection, DataStorageInstanceType instanceType) : base(connection,
        instanceType)
    {
    }
}
