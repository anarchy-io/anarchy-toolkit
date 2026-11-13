namespace Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

/// <summary>
/// Utility to get Oracle connection strings. The overload resolving the host from the environment being built in
/// lives in <c>OracleConnectionStringTasks</c> (Configuration layer), as it depends on the build components.
/// </summary>
public static class OracleConnectionStringUtility
{
    /// <summary>
    /// Gets an ADO.NET Oracle connection string based on host, username, and password. By default the
    /// <paramref name="serviceName"/> used is <c>SERVICES</c>.
    /// </summary>
    public static string GetOracleConnectionString(
        string host,
        string username,
        string password,
        string serviceName = "SERVICES"
    ) =>
        $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCPS)(HOST={host})(PORT=2484))(CONNECT_DATA=(SERVICE_NAME={serviceName})));User ID={username};Password=\"{password}\"";
}
