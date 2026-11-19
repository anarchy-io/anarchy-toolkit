using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Entity.Configuration.Storage.Database;

/// <summary>Builds a MySqlConnector connection string for the selected reader or writer.</summary>
/// <remarks>
/// <para>
/// Besides the options, the string fixes: a 10 second connection timeout, pooling with a minimum of 1 connection and
/// <c>MaximumPoolSize</c> as maximum, a 60 second idle timeout, 30 second TCP keepalive and
/// <c>Ignore Command Transaction=true</c> (the shared repository layer associates commands with the connection, not
/// the transaction object). Values are escaped for the connection string syntax.
/// </para>
/// <para>
/// The session time zone has no connection string key; it is exposed through <see cref="SessionTimeZone"/> and
/// applied by <c>MysqlConnectionPool</c> when a connection is opened.
/// </para>
/// </remarks>
/// <param name="options">The MySQL connection options.</param>
public class MysqlConnectionString(MysqlConnectionOptions options) : DbConnectionStringBase<MysqlConnectionOptions>(options)
{
    /// <summary>Port used when <c>Port</c> is not set: 3306.</summary>
    public const uint DefaultPort = 3306;

    /// <inheritdoc/>
    protected override string ConfigError => "Invalid Mysql connection settings";

    /// <summary>Session time zone to set on every connection, or <see langword="null"/> to keep the server default.</summary>
    public string? SessionTimeZone => string.IsNullOrWhiteSpace(Options.TimeZone) ? null : Options.TimeZone;

    /// <inheritdoc/>
    protected override IEnumerable<KeyValuePair<string, object>> ConnectionSettings(Uri host)
    {
        return new Dictionary<string, object> {
            { "Server", Escape(host.IsAbsoluteUri ? host.Host : host.OriginalString) },
            { "Port", Options.Port ?? DefaultPort },
            { "User ID", Escape(Options.Username) },
            { "Password", Escape(Options.Password) },
            { "Database", Escape(Options.DbName) },
            { "Default Command Timeout", Math.Max(1, Options.CommandTimeout) },
            { "Connection Timeout", 10 },
            // The shared repository layer associates commands with the active connection, not its transaction object.
            { "Ignore Command Transaction", true },
            { "Pooling", true },
            { "Minimum Pool Size", 1 },
            { "Maximum Pool Size", Options.MaximumPoolSize },
            { "Connection Idle Timeout", 60 },
            { "Keepalive", 30 }
        };
    }

    private static string Escape(string value)
    {
        var builder = new DbConnectionStringBuilder { ["value"] = value };
        return builder.ConnectionString["value=".Length..];
    }
}
