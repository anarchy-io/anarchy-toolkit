using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Repository.Storage.Database.Postgres;

public abstract class PostgresDbReaderRepositoryBase<TPersistedEntity>(
    IStoragePolicy<NpgsqlConnection> dbPolicy,
    Observer observer)
    : DbReaderRepositoryBase<NpgsqlConnection, PostgresDbDialect, TPersistedEntity>(dbPolicy, observer)
    where TPersistedEntity : IPersistedEntity;
