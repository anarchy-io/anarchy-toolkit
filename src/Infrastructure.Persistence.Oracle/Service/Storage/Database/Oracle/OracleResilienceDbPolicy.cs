using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;

public class OracleResilienceDbPolicy(
    IAsyncRetryPolicyBuilder<OracleConnection> retryPolicyBuilder,
    IDbConnectionPool<OracleConnection> dbConnectionPool,
    IAsyncFlowContext<StorageConnectionEnvelopeBase<OracleConnection>> asyncFlowContext,
    Observer? observer = null)
    : ResilienceDbPolicy<OracleConnection>(retryPolicyBuilder, dbConnectionPool, asyncFlowContext, observer);
