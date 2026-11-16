using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Health.Report.Oracle;

/// <summary>
///     Health report that verifies connectivity to the Oracle reader instance.
/// </summary>
/// <param name="dbPolicy">The keyed <c>"reader"</c> storage policy.</param>
/// <param name="observer">Observer used to log failures.</param>
public class OracleHealthReportReader(
    [FromKeyedServices("reader")] IStoragePolicy<OracleConnection> dbPolicy,
    Observer observer) : OracleHealthReportBase(dbPolicy, observer, DataStorageInstanceType.Reader);
