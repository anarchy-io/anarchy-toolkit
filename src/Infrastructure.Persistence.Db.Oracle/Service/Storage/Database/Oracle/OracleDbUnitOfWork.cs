using Anarchy.Toolkit.Core.Events.Service.Event.InMemory;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Storage.Database.Oracle;

/// <summary>
/// Oracle unit of work: runs callbacks in a session sharing a single writer connection and transaction
/// (<see cref="OracleTransaction"/>) across the repositories called within it.
/// </summary>
/// <remarks>
/// See <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbUnitOfWorkBase{TDbConnection, TDbTransaction}"/> for the session
/// flavours and constraints. Registered as a scoped <c>IUnitOfWork</c> by <c>ModuleOracle</c>.
/// </remarks>
/// <param name="dbConnectionPool">The keyed <c>"writer"</c> connection pool.</param>
/// <param name="asyncFlowContext">Async flow context shared with the storage policies so repositories join the session.</param>
/// <param name="telemetry">Optional application telemetry used for logging and metrics.</param>
/// <param name="localEventBus">Optional bus on which transaction lifecycle events are published.</param>
public class OracleDbUnitOfWork(
    [FromKeyedServices("writer")] IDbConnectionPool<OracleConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<OracleConnection>> asyncFlowContext,
    IAppTelemetry? telemetry = null,
    ILocalEventBus? localEventBus = null)
    : DbUnitOfWorkBase<OracleConnection, OracleTransaction>(
        dbConnectionPool,
        asyncFlowContext,
        telemetry,
        localEventBus);
