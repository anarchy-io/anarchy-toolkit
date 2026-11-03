using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;

public class PostgresAsyncRetryPolicyBuilder(PostgresResilienceOptions options, Observer observer)
    : DbAsyncRetryPolicyBuilder<NpgsqlConnection, PostgresDbDialect>(options, observer);
