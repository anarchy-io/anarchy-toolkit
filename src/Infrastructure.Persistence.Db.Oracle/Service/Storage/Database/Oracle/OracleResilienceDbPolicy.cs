using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Storage.Database.Oracle;

/// <summary>
/// Oracle flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.ResilienceDbPolicy{TDbConnection}"/>: executes database
/// callbacks with retries, using the active unit-of-work connection when present or a pooled one otherwise.
/// </summary>
/// <remarks>
/// <c>ModuleOracle</c> registers one instance per instance type, keyed <c>"writer"</c> and <c>"reader"</c>, as
/// <c>IStoragePolicy&lt;OracleConnection&gt;</c>; the unkeyed registration resolves to the writer.
/// </remarks>
/// <param name="retryPolicyBuilder">Builder of the retry pipeline.</param>
/// <param name="dbConnectionPool">Connection pool used when no unit of work session is active.</param>
/// <param name="asyncFlowContext">Async flow context holding the connection of the active unit of work session, if any.</param>
/// <param name="telemetry">Optional application telemetry used for logging and metrics.</param>
public class OracleResilienceDbPolicy(
    IAsyncRetryPolicyBuilder<OracleConnection> retryPolicyBuilder,
    IDbConnectionPool<OracleConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<OracleConnection>> asyncFlowContext,
    IAppTelemetry? telemetry = null)
    : ResilienceDbPolicy<OracleConnection>(retryPolicyBuilder, dbConnectionPool, asyncFlowContext, telemetry);
