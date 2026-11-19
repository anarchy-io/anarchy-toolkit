using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Health.Report.Oracle;

/// <summary>
///     Health report that verifies connectivity to the Oracle writer instance.
/// </summary>
/// <param name="dbPolicy">The keyed <c>"writer"</c> storage policy.</param>
/// <param name="telemetry">Application telemetry used to log failures.</param>
public class OracleHealthReportWriter(
    [FromKeyedServices("writer")] IStoragePolicy<OracleConnection> dbPolicy,
    IAppTelemetry telemetry) : OracleHealthReportBase(dbPolicy, telemetry, DataStorageInstanceType.Writer);
