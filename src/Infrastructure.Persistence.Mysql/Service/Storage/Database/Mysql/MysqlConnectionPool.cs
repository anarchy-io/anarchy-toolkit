using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;

/// <summary>
/// MySQL connection pool for one instance type (reader or writer), built on
/// <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbConnectionPoolBase{TDbConnection}"/>.
/// </summary>
/// <remarks>
/// Registered as a keyed singleton (<c>"writer"</c>/<c>"reader"</c>) by <c>ModuleMysql</c>.
/// </remarks>
/// <param name="instanceType">The instance type the pool connects to.</param>
/// <param name="connectionString">Builds the connection string and provides the session time zone.</param>
public class MysqlConnectionPool(DataStorageInstanceType instanceType, MysqlConnectionString connectionString)
    : DbConnectionPoolBase<MySqlConnection>(instanceType, connectionString.Value(instanceType))
{
    /// <inheritdoc/>
    protected override MySqlConnection CreateConnection(string value) => new(value);

    /// <summary>
    /// Runs <c>SET time_zone = @timeZone</c> when a session time zone is configured. On failure the connection is
    /// disposed and the exception rethrown.
    /// </summary>
    /// <param name="connection">The connection just opened.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the session has been prepared.</returns>
    protected override async Task OnOpenedAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        if (connectionString.SessionTimeZone is not { } timeZone) return;
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SET time_zone = @timeZone";
            command.Parameters.AddWithValue("@timeZone", timeZone);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    /// <returns>A new <see cref="MysqlConnectionEnvelope"/>.</returns>
    protected override StorageConnectionEnvelopeBase<MySqlConnection> WrapConnection(MySqlConnection connection) =>
        new MysqlConnectionEnvelope(connection, InstanceType);
}
