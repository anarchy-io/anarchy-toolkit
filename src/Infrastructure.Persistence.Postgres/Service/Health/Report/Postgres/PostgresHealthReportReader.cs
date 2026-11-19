using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Health.Report.Postgres;

/// <summary>
///     Health report that verifies connectivity to the PostgreSQL reader instance.
/// </summary>
/// <param name="dbPolicy">The keyed <c>"reader"</c> storage policy.</param>
/// <param name="telemetry">Application telemetry used to log failures.</param>
public class PostgresHealthReportReader(
    [FromKeyedServices("reader")] IStoragePolicy<NpgsqlConnection> dbPolicy,
    IAppTelemetry telemetry) : PostgresHealthReportBase(dbPolicy, telemetry, DataStorageInstanceType.Reader);
