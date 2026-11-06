using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;
using AwesomeAssertions;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Oracle.Infrastructure.Entity.Configuration.Storage.Database;

public class OracleConnectionStringTest
{
    private static OracleConnectionOptions Options(
        string readerHost = "reader.local",
        uint? port = null,
        string password = "secret",
        string timeZone = "Europe/Amsterdam",
        int commandTimeout = 0) => new(
        new Uri(readerHost, UriKind.RelativeOrAbsolute), new Uri("tcp://writer.local"), "app_user", password,
        "ORDERS", timeZone, port, commandTimeout);

    [Theory]
    [InlineData(DataStorageInstanceType.Reader, "reader.local")]
    [InlineData(DataStorageInstanceType.Writer, "writer.local")]
    public void Value_BuildsConnectDescriptorForHostOfInstanceType(DataStorageInstanceType instanceType,
                                                                  string expectedHost)
    {
        var builder = new OracleConnectionStringBuilder(new OracleConnectionString(Options()).Value(instanceType));

        builder.UserID.Should().Be("app_user");
        builder.Password.Should().Be("secret");
        builder.DataSource.Should().Be(
            $"(DESCRIPTION=(EXPIRE_TIME=1)(CONNECT_TIMEOUT=10)(ADDRESS=(PROTOCOL=TCP)(HOST={expectedHost})(PORT=1521))"
            + "(CONNECT_DATA=(SERVICE_NAME=ORDERS)))");
        builder.ConnectionTimeout.Should().Be(10);
        builder.Pooling.Should().BeTrue();
    }

    [Fact]
    public void Value_UsesConfiguredPort()
    {
        new OracleConnectionString(Options(port: 1522)).Value(DataStorageInstanceType.Writer)
            .Should().Contain("(PORT=1522)");
    }

    [Theory]
    [InlineData(DataStorageInstanceType.Reader)]
    [InlineData(DataStorageInstanceType.Writer)]
    public void Value_UsesDefaultAndConfiguredMaximumPoolSize(DataStorageInstanceType instanceType)
    {
        new OracleConnectionStringBuilder(new OracleConnectionString(Options()).Value(instanceType))
            .MaxPoolSize.Should().Be(100);
        new OracleConnectionStringBuilder(new OracleConnectionString(Options() with { MaximumPoolSize = 17 }).Value(instanceType))
            .MaxPoolSize.Should().Be(17);
    }

    [Fact]
    public void Value_AcceptsAbsoluteHostUris()
    {
        new OracleConnectionString(Options(readerHost: "tcp://replica.local:1521")).Value(DataStorageInstanceType.Reader)
            .Should().Contain("(HOST=replica.local)");
    }

    [Fact]
    public void CommandTimeoutAndSessionTimeZone_AreExposedForThePool()
    {
        var connectionString = new OracleConnectionString(Options(commandTimeout: 0));

        connectionString.CommandTimeout.Should().Be(1);
        connectionString.SessionTimeZone.Should().Be("Europe/Amsterdam");
        new OracleConnectionString(Options(timeZone: " ")).SessionTimeZone.Should().BeNull();
    }

    [Fact]
    public void Value_WhenPasswordIsEmpty_Throws()
    {
        var act = () => new OracleConnectionString(Options(password: "")).Value(DataStorageInstanceType.Writer);

        act.Should().Throw<EmptyArgumentException>().WithMessage("*Invalid Oracle connection settings*");
    }
}
