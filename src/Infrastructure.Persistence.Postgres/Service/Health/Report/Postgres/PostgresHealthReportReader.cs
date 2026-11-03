using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Health.Report.Postgres;

/// <summary>
///     Health report that verifies connectivity to the PostgreSQL reader instance.
/// </summary>
public class PostgresHealthReportReader(
    [FromKeyedServices("reader")] IStoragePolicy<NpgsqlConnection> dbPolicy,
    Observer observer) : PostgresHealthReportBase(dbPolicy, observer, DataStorageInstanceType.Reader);
