using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Health.Report.Postgres;

/// <summary>
///     Health report that verifies connectivity to the PostgreSQL writer instance.
/// </summary>
/// <param name="dbPolicy">The keyed <c>"writer"</c> storage policy.</param>
/// <param name="telemetry">Application telemetry used to log failures.</param>
public class PostgresHealthReportWriter(
    [FromKeyedServices("writer")] IStoragePolicy<NpgsqlConnection> dbPolicy,
    AppTelemetry telemetry) : PostgresHealthReportBase(dbPolicy, telemetry, DataStorageInstanceType.Writer);
