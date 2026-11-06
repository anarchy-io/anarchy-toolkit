using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;
using System.Data.Common;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;

/// <summary>Builds a MySqlConnector connection string for the selected reader or writer.</summary>
public class MysqlConnectionString(MysqlConnectionOptions options) : DbConnectionStringBase<MysqlConnectionOptions>(options)
{
    public const uint DefaultPort = 3306;
    protected override string ConfigError => "Invalid Mysql connection settings";
    public string? SessionTimeZone => string.IsNullOrWhiteSpace(Options.TimeZone) ? null : Options.TimeZone;

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
