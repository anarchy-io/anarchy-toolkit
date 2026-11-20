using Anarchy.Toolkit.Core.Events.Local;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql;

/// <summary>
/// MySQL unit of work: runs callbacks in a session sharing a single writer connection and transaction
/// (<see cref="MySqlTransaction"/>) across the repositories called within it.
/// </summary>
/// <remarks>
/// See <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbUnitOfWorkBase{TDbConnection, TDbTransaction}"/> for the session
/// flavours and constraints. Registered as a scoped <c>IUnitOfWork</c> by <c>ModuleMysql</c>.
/// </remarks>
/// <param name="dbConnectionPool">The keyed <c>"writer"</c> connection pool.</param>
/// <param name="asyncFlowContext">Async flow context shared with the storage policies so repositories join the session.</param>
/// <param name="telemetry">Optional application telemetry used for logging and metrics.</param>
/// <param name="localEventBus">Optional bus on which transaction lifecycle events are published.</param>
public class MysqlDbUnitOfWork(
    [FromKeyedServices("writer")] IDbConnectionPool<MySqlConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>> asyncFlowContext,
    AppTelemetry? telemetry = null, ILocalEventBus? localEventBus = null)
    : DbUnitOfWorkBase<MySqlConnection, MySqlTransaction>(dbConnectionPool, asyncFlowContext, telemetry, localEventBus);
