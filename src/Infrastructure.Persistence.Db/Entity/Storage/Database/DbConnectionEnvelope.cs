using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database;

/// <summary>
/// Default <see cref="StorageConnectionEnvelopeBase{TDbConnection}"/> for ADO.NET connections, assigning a new GUID as
/// connection identifier and the current time as start date.
/// </summary>
/// <typeparam name="TDbConnection">Type of the wrapped database connection.</typeparam>
/// <remarks>
/// Created by <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbConnectionPoolBase{TDbConnection}"/>
/// every time a connection is handed out.
/// </remarks>
public record DbConnectionEnvelope<TDbConnection> : StorageConnectionEnvelopeBase<TDbConnection>
    where TDbConnection : DbConnection
{
    /// <summary>Wraps an opened connection.</summary>
    /// <param name="connection">The opened database connection.</param>
    /// <param name="instanceType">Whether the connection targets a reader or a writer instance.</param>
    public DbConnectionEnvelope(TDbConnection connection, DataStorageInstanceType instanceType) : base(connection,
        instanceType, Guid.NewGuid().ToString(), DateHelper.Now())
    {
    }
}
