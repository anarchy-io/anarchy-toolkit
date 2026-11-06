using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Storage.Database;

public sealed record MysqlConnectionEnvelope(MySqlConnection Connection, DataStorageInstanceType InstanceType)
    : DbConnectionEnvelope<MySqlConnection>(Connection, InstanceType);
