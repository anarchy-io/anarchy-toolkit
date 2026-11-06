using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Repository.Storage.Database.Mysql;

public abstract class MysqlDbRepositoryBase(IStoragePolicy<MySqlConnection> dbPolicy, Observer observer)
    : DbStorageRepositoryBase<MySqlConnection, MysqlDbDialect>(dbPolicy, observer);
