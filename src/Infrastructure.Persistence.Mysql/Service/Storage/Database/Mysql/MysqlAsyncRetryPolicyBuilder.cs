using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;

public class MysqlAsyncRetryPolicyBuilder(MysqlResilienceOptions options, Observer observer)
    : DbAsyncRetryPolicyBuilder<MySqlConnection, MysqlDbDialect>(options, observer);
