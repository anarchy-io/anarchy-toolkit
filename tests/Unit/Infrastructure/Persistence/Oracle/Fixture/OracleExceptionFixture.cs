using Oracle.ManagedDataAccess.Client;
using System.Reflection;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Oracle.Fixture;

/// <summary>
/// Builds <see cref="OracleException"/> instances, which ODP.NET only creates internally.
/// </summary>
public static class OracleExceptionFixture
{
    public static OracleException Create(int errorNumber, string message = "failure")
    {
        var constructor = typeof(OracleException).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(int), typeof(string), typeof(string), typeof(string), typeof(Exception)])!;

        return (OracleException)constructor.Invoke([errorNumber, "test", "test", message, new Exception(message)]);
    }
}
