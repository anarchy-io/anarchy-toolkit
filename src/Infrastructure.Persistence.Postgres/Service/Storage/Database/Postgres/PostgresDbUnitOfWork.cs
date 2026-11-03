using Anarchy.Toolkit.Core.Events.Service.Event.InMemory;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;

public class PostgresDbUnitOfWork(
    [FromKeyedServices("writer")] IDbConnectionPool<NpgsqlConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<NpgsqlConnection>> asyncFlowContext,
    Observer? observer = null,
    ILocalEventBus? localEventBus = null)
    : DbUnitOfWorkBase<NpgsqlConnection, NpgsqlTransaction>(
        dbConnectionPool,
        asyncFlowContext,
        observer,
        localEventBus);
