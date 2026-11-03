using Anarchy.Toolkit.Build.Nuke.Infrastructure.Helper;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Build.Nuke.Infrastructure.Helper;

public class ConnectionStringUtilityTest
{
    [Fact]
    public void GetOracleConnectionString_GivenHostAndCredentials_ReturnsTcpsConnectionString()
    {
        // Act
        var result = OracleConnectionStringUtility.GetOracleConnectionString("db-host", "user", "p@ss");

        // Assert
        result.Should().Be(
            "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCPS)(HOST=db-host)(PORT=2484))(CONNECT_DATA=(SERVICE_NAME=SERVICES)));User ID=user;Password=\"p@ss\"");
    }

    [Fact]
    public void GetMySqlConnectionString_GivenNoPort_UsesDefaultPort()
    {
        // Act
        var result = MySqlConnectionStringUtility.GetMySqlConnectionString("db-host", "orders", "user", "secret");

        // Assert
        result.Should().Be("Server=db-host;Port=3306;Database=orders;Uid=user;Pwd=secret");
    }
}
