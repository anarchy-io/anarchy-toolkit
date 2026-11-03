using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;

public class OracleAsyncRetryPolicyBuilder(OracleResilienceOptions options, Observer observer)
    : DbAsyncRetryPolicyBuilder<OracleConnection, OracleDbDialect>(options, observer);
