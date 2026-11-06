using Anarchy.Toolkit.Core.Events.Service.Event.InMemory;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;

public class MysqlDbUnitOfWork(
    [FromKeyedServices("writer")] IDbConnectionPool<MySqlConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>> asyncFlowContext,
    Observer? observer = null, ILocalEventBus? localEventBus = null)
    : DbUnitOfWorkBase<MySqlConnection, MySqlTransaction>(dbConnectionPool, asyncFlowContext, observer, localEventBus);
