using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Storage.Database;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;

public class OracleConnectionPool(DataStorageInstanceType instanceType, OracleConnectionString connectionString)
    : DbConnectionPoolBase<OracleConnection>(instanceType, connectionString.Value(instanceType))
{
    protected override OracleConnection CreateConnection(string connectionStringValue)
    {
        return new OracleConnection(connectionStringValue) { CommandTimeout = connectionString.CommandTimeout };
    }

    /// <summary>Applies the configured session time zone, when it differs from the session's current one.</summary>
    protected override Task OnOpenedAsync(OracleConnection connection, CancellationToken cancellationToken)
    {
        var timeZone = connectionString.SessionTimeZone;

        if (timeZone is null)
        {
            return Task.CompletedTask;
        }

        var sessionInfo = connection.GetSessionInfo();

        if (!string.Equals(sessionInfo.TimeZone, timeZone, StringComparison.OrdinalIgnoreCase))
        {
            sessionInfo.TimeZone = timeZone;
            connection.SetSessionInfo(sessionInfo);
        }

        return Task.CompletedTask;
    }

    protected override StorageConnectionEnvelopeBase<OracleConnection> WrapConnection(OracleConnection connection)
    {
        return new OracleConnectionEnvelope(connection, InstanceType);
    }
}
