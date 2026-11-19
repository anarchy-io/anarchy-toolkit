using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Entity.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Storage.Database.Oracle;

/// <summary>
/// Oracle connection pool for one instance type (reader or writer), built on
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbConnectionPoolBase{TDbConnection}"/>.
/// </summary>
/// <remarks>
/// Applies <see cref="OracleConnectionString.CommandTimeout"/> to every new connection and
/// <see cref="OracleConnectionString.SessionTimeZone"/> to every opened session. Registered as a keyed singleton
/// (<c>"writer"</c>/<c>"reader"</c>) by <c>ModuleOracle</c>.
/// </remarks>
/// <param name="instanceType">The instance type the pool connects to.</param>
/// <param name="connectionString">Builds the connection string and provides the command timeout and session time zone.</param>
public class OracleConnectionPool(DataStorageInstanceType instanceType, OracleConnectionString connectionString)
    : DbConnectionPoolBase<OracleConnection>(instanceType, connectionString.Value(instanceType))
{
    /// <inheritdoc/>
    /// <remarks>Sets <see cref="OracleConnection.CommandTimeout"/> from the connection string settings.</remarks>
    protected override OracleConnection CreateConnection(string connectionStringValue)
    {
        return new OracleConnection(connectionStringValue) { CommandTimeout = connectionString.CommandTimeout };
    }

    /// <summary>Applies the configured session time zone, when it differs from the session's current one.</summary>
    /// <param name="connection">The connection just opened.</param>
    /// <param name="cancellationToken">Token used to cancel the operation (not used; the call is synchronous).</param>
    /// <returns>A completed task.</returns>
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

    /// <inheritdoc/>
    /// <returns>A new <see cref="OracleConnectionEnvelope"/>.</returns>
    protected override StorageConnectionEnvelopeBase<OracleConnection> WrapConnection(OracleConnection connection)
    {
        return new OracleConnectionEnvelope(connection, InstanceType);
    }
}
