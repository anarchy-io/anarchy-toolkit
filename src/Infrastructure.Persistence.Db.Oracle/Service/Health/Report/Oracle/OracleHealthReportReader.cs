using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Health.Report.Oracle;

/// <summary>
///     Health report that verifies connectivity to the Oracle reader instance.
/// </summary>
/// <param name="dbPolicy">The keyed <c>"reader"</c> storage policy.</param>
/// <param name="telemetry">Application telemetry used to log failures.</param>
public class OracleHealthReportReader(
    [FromKeyedServices("reader")] IStoragePolicy<OracleConnection> dbPolicy,
    IAppTelemetry telemetry) : OracleHealthReportBase(dbPolicy, telemetry, DataStorageInstanceType.Reader);
