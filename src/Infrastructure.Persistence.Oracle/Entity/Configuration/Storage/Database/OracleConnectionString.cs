using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;

/// <summary>
///     Builds an Oracle (ODP.NET) connection string from <see cref="OracleConnectionOptions" />, selecting the
///     reader or writer host based on the requested <see cref="DataStorageInstanceType" />.
/// </summary>
/// <remarks>
///     ODP.NET has no connection string keys for the command timeout or session time zone; they are exposed through
///     <see cref="CommandTimeout" /> and <see cref="SessionTimeZone" /> and applied by the connection pool.
/// </remarks>
public class OracleConnectionString(OracleConnectionOptions options)
    : DbConnectionStringBase<OracleConnectionOptions>(options)
{
    public const uint DefaultPort = 1521;

    protected override string ConfigError => "Invalid Oracle connection settings";

    /// <summary>Command timeout (seconds) to set on every connection.</summary>
    public int CommandTimeout => Math.Max(1, Options.CommandTimeout);

    /// <summary>Session time zone to set on every connection, or <c>null</c> to keep the client default.</summary>
    public string? SessionTimeZone => string.IsNullOrWhiteSpace(Options.TimeZone) ? null : Options.TimeZone;

    protected override IEnumerable<KeyValuePair<string, object>> ConnectionSettings(Uri host)
    {
        var hostName = host.IsAbsoluteUri ? host.Host : host.OriginalString;

        return new Dictionary<string, object> {
            { "User Id", Options.Username },
            { "Password", Options.Password },
            // Full connect descriptor rather than EZConnect so the client-side EXPIRE_TIME (minutes) can be set: it
            // sends TCP keepalive probes, detecting half-open connections (common after an AWS Lambda freeze/thaw)
            // instead of blocking on a dead socket until the OS TCP retransmit timeout elapses.
            {
                "Data Source",
                $"(DESCRIPTION=(EXPIRE_TIME=1)(CONNECT_TIMEOUT=10)"
                + $"(ADDRESS=(PROTOCOL=TCP)(HOST={hostName})(PORT={Options.Port ?? DefaultPort}))"
                + $"(CONNECT_DATA=(SERVICE_NAME={Options.DbName})))"
            },
            // Bound how long establishing a physical connection may block so a dead endpoint fails fast
            // instead of stalling the whole request.
            { "Connection Timeout", 10 },
            { "Pooling", true },
            { "Min Pool Size", 1 },
            { "Max Pool Size", 100 },
            // Retire pooled physical connections after a bounded lifetime so the pool does not hold stale sessions.
            { "Connection Lifetime", 300 }
        };
    }
}
