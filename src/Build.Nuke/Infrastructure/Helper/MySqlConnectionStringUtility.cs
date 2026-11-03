
namespace Anarchy.Toolkit.Build.Nuke.Infrastructure.Helper
{
    /// <summary>
    /// Utility to get MySQL connection strings
    /// </summary>
    public static class MySqlConnectionStringUtility
    {
        public const int DefaultPort = 3306;

        /// <summary>
        /// Gets an ADO.NET MySQL connection string based on server, database, username, password and port.
        /// By default the <paramref name="port"/> used is <c>3306</c>
        /// </summary>
        public static string GetMySqlConnectionString(string server, string database, string username, string password, int port = DefaultPort) =>
            $"Server={server};Port={port};Database={database};Uid={username};Pwd={password}";
    }
}
