namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

/// <summary>
/// Utility to get MySQL connection strings.
/// </summary>
public static class MySqlConnectionStringUtility
{
    /// <summary>
    /// The default MySQL port.
    /// </summary>
    public const int DefaultPort = 3306;

    /// <summary>
    /// Gets an ADO.NET MySQL connection string based on server, database, username, password and port.
    /// By default the <paramref name="port"/> used is <see cref="DefaultPort"/> (<c>3306</c>).
    /// </summary>
    /// <param name="server">The server host.</param>
    /// <param name="database">The database name.</param>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="port">The server port.</param>
    /// <returns>The connection string.</returns>
    public static string GetMySqlConnectionString(string server, string database, string username, string password, int port = DefaultPort) =>
        $"Server={server};Port={port};Database={database};Uid={username};Pwd={password}";
}
