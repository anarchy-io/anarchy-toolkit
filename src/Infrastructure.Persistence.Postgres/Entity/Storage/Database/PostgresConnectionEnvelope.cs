using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Storage.Database;

/// <summary>
/// Connection envelope wrapping a <see cref="NpgsqlConnection"/> together with the instance type (reader or writer) it was
/// opened against.
/// </summary>
/// <remarks>Created by <c>PostgresConnectionPool</c> for every connection handed out.</remarks>
public sealed record PostgresConnectionEnvelope : DbConnectionEnvelope<NpgsqlConnection>
{
    /// <summary>Initializes a new envelope for an opened connection.</summary>
    /// <param name="npgsql">The opened connection.</param>
    /// <param name="instanceType">The instance type (reader or writer) the connection targets.</param>
    public PostgresConnectionEnvelope(NpgsqlConnection npgsql, DataStorageInstanceType instanceType) : base(npgsql,
        instanceType)
    {
    }
}
