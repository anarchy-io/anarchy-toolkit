using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Entity.Storage.Database;

/// <summary>
/// Connection envelope wrapping a <see cref="OracleConnection"/> together with the instance type (reader or writer) it was
/// opened against.
/// </summary>
/// <remarks>Created by <c>OracleConnectionPool</c> for every connection handed out.</remarks>
public sealed record OracleConnectionEnvelope : DbConnectionEnvelope<OracleConnection>
{
    /// <summary>Initializes a new envelope for an opened connection.</summary>
    /// <param name="connection">The opened connection.</param>
    /// <param name="instanceType">The instance type (reader or writer) the connection targets.</param>
    public OracleConnectionEnvelope(OracleConnection connection, DataStorageInstanceType instanceType) : base(connection,
        instanceType)
    {
    }
}
