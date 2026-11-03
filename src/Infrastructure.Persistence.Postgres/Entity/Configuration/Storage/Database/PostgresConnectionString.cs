using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;

/// <summary>
///     Builds a Postgres connection string from <see cref="PostgresConnectionOptions" />, selecting the
///     reader or writer host based on the requested <see cref="DataStorageInstanceType" />.
/// </summary>
public class PostgresConnectionString(PostgresConnectionOptions options)
    : DbConnectionStringBase<PostgresConnectionOptions>(options)
{
    protected override string ConfigError => "Invalid Postgres connection settings";

    protected override IEnumerable<KeyValuePair<string, object>> ConnectionSettings(Uri host)
    {
        return new Dictionary<string, object> {
            { "Host", host },
            { "Port", Options.Port ?? 5432 },
            { "Username", Options.Username },
            { "Password", Options.Password },
            { "Database", Options.DbName },
            { "TimeZone", Options.TimeZone },
            { "CommandTimeout", Math.Max(1, Options.CommandTimeout) },
            // Bound how long establishing a physical connection may block so a dead endpoint fails fast
            // instead of stalling the whole request.
            { "Timeout", 10 },
            { "Pooling", true },
            { "MinPoolSize", 1 },
            { "MaxPoolSize", 100 },
            // Detect and evict half-open TCP connections (common after an AWS Lambda freeze/thaw) instead of
            // blocking on a dead socket until the OS TCP retransmit timeout elapses. Keepalive probes and a
            // bounded idle lifetime keep the pool populated only with live connections.
            { "Keepalive", 30 },
            { "Tcp Keepalive", true },
            { "Connection Idle Lifetime", 60 },
            { "Connection Pruning Interval", 10 }
        };
    }
}
