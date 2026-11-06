using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;
using AwesomeAssertions;
using Npgsql;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Postgres.Infrastructure.Entity.Configuration.Storage.Database;

public class PostgresConnectionStringTest
{
    private static readonly Uri ReaderHost = new("tcp://reader.local");
    private static readonly Uri WriterHost = new("tcp://writer.local");

    private static PostgresConnectionOptions Options(uint? port = null, string password = "secret") => new(
        ReaderHost, WriterHost, "user", password, "orders", "UTC", port, 0);

    [Theory]
    [InlineData(DataStorageInstanceType.Reader)]
    [InlineData(DataStorageInstanceType.Writer)]
    public void Value_UsesHostOfInstanceTypeAndPostgresSettings(DataStorageInstanceType instanceType)
    {
        var value = new PostgresConnectionString(Options()).Value(instanceType);
        var expectedHost = instanceType == DataStorageInstanceType.Reader ? ReaderHost : WriterHost;

        value.Should().StartWith($"Host={expectedHost};Port=5432;Username=user;Password=secret;Database=orders;TimeZone=UTC;CommandTimeout=1;");
        value.Should().Contain("Tcp Keepalive=True");
    }

    [Fact]
    public void Value_UsesConfiguredPort()
    {
        new PostgresConnectionString(Options(port: 6543)).Value(DataStorageInstanceType.Writer)
            .Should().Contain("Port=6543;");
    }

    [Theory]
    [InlineData(DataStorageInstanceType.Reader)]
    [InlineData(DataStorageInstanceType.Writer)]
    public void Value_UsesDefaultAndConfiguredMaximumPoolSize(DataStorageInstanceType instanceType)
    {
        new NpgsqlConnectionStringBuilder(new PostgresConnectionString(Options()).Value(instanceType))
            .MaxPoolSize.Should().Be(100);
        new NpgsqlConnectionStringBuilder(new PostgresConnectionString(Options() with { MaximumPoolSize = 17 }).Value(instanceType))
            .MaxPoolSize.Should().Be(17);
    }

    [Fact]
    public void Value_WhenPasswordIsEmpty_Throws()
    {
        var act = () => new PostgresConnectionString(Options(password: "")).Value(DataStorageInstanceType.Writer);

        act.Should().Throw<EmptyArgumentException>().WithMessage("*Invalid Postgres connection settings*");
    }
}
