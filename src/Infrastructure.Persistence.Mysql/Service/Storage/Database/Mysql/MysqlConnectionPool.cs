using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;

public class MysqlConnectionPool(DataStorageInstanceType instanceType, MysqlConnectionString connectionString)
    : DbConnectionPoolBase<MySqlConnection>(instanceType, connectionString.Value(instanceType))
{
    protected override MySqlConnection CreateConnection(string value) => new(value);

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

    protected override StorageConnectionEnvelopeBase<MySqlConnection> WrapConnection(MySqlConnection connection) =>
        new MysqlConnectionEnvelope(connection, InstanceType);
}
