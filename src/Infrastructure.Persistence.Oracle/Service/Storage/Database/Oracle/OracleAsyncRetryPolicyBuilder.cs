using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;

/// <summary>
/// Oracle flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database.DbAsyncRetryPolicyBuilder{TDbConnection, TDialect}"/>:
/// builds the exponential back-off retry pipeline for exceptions that <see cref="OracleDbDialect"/> classifies as transient.
/// </summary>
/// <remarks>Registered as a singleton by <c>ModuleOracle</c>.</remarks>
/// <param name="options">Resilience options; <c>RetryCount</c> sets the maximum number of retries.</param>
/// <param name="telemetry">Application telemetry used to log a warning on every retry.</param>
public class OracleAsyncRetryPolicyBuilder(OracleResilienceOptions options, IAppTelemetry? telemetry = null)
    : DbAsyncRetryPolicyBuilder<OracleConnection, OracleDbDialect>(options, telemetry);
