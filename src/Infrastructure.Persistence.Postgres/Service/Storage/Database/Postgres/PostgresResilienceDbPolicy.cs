using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;

public class PostgresResilienceDbPolicy(
    IAsyncRetryPolicyBuilder<NpgsqlConnection> retryPolicyBuilder,
    IDbConnectionPool<NpgsqlConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<NpgsqlConnection>> asyncFlowContext,
    Observer? observer = null)
    : ResilienceDbPolicy<NpgsqlConnection>(retryPolicyBuilder, dbConnectionPool, asyncFlowContext, observer);
