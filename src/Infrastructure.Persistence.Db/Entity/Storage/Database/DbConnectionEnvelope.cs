using Anarchy.Toolkit.Core.Helper;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database;

public record DbConnectionEnvelope<TDbConnection> : StorageConnectionEnvelopeBase<TDbConnection>
    where TDbConnection : DbConnection
{
    public DbConnectionEnvelope(TDbConnection connection, DataStorageInstanceType instanceType) : base(connection,
        instanceType, Guid.NewGuid().ToString(), DateHelper.Now())
    {
    }
}
