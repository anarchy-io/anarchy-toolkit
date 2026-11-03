using Anarchy.Toolkit.Core.Events.Service.Event.InMemory;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;

public class OracleDbUnitOfWork(
    [FromKeyedServices("writer")] IDbConnectionPool<OracleConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<OracleConnection>> asyncFlowContext,
    Observer? observer = null,
    ILocalEventBus? localEventBus = null)
    : DbUnitOfWorkBase<OracleConnection, OracleTransaction>(
        dbConnectionPool,
        asyncFlowContext,
        observer,
        localEventBus);
