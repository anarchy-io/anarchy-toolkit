using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Storage.Database;

/// <summary>
/// Connection envelope wrapping a <see cref="MySqlConnection"/> together with the instance type (reader or writer) it was
/// opened against.
/// </summary>
/// <param name="Connection">The opened MySQL connection.</param>
/// <param name="InstanceType">The instance type (reader or writer) the connection targets.</param>
/// <remarks>Created by <c>MysqlConnectionPool</c> for every connection handed out.</remarks>
public sealed record MysqlConnectionEnvelope(MySqlConnection Connection, DataStorageInstanceType InstanceType)
    : DbConnectionEnvelope<MySqlConnection>(Connection, InstanceType);
