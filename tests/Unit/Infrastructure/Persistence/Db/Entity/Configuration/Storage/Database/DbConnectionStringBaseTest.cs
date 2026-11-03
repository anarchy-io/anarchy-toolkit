using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Entity.Configuration.Storage.Database;

public class DbConnectionStringBaseTest
{
    private sealed class DummyConnectionString(DbConnectionOptions options)
        : DbConnectionStringBase<DbConnectionOptions>(options)
    {
        protected override IEnumerable<KeyValuePair<string, object>> ConnectionSettings(Uri host)
        {
            return new Dictionary<string, object> {
                { "Host", host.Host },
                { "Port", Options.Port ?? 1 },
                { "Database", Options.DbName }
            };
        }
    }

    private static DbConnectionOptions Options(string username = "user") => new(
        new Uri("tcp://reader.local"),
        new Uri("tcp://writer.local"),
        username,
        "password",
        "db",
        null,
        30);

    [Theory]
    [InlineData(DataStorageInstanceType.Reader, "reader.local")]
    [InlineData(DataStorageInstanceType.Writer, "writer.local")]
    public void Value_SelectsHostByInstanceType(DataStorageInstanceType instanceType, string expectedHost)
    {
        new DummyConnectionString(Options()).Value(instanceType)
            .Should().Be($"Host={expectedHost};Port=1;Database=db");
    }

    [Fact]
    public void Value_WhenRequiredSettingIsEmpty_Throws()
    {
        var act = () => new DummyConnectionString(Options(username: " ")).Value(DataStorageInstanceType.Writer);

        act.Should().Throw<EmptyArgumentException>();
    }

    [Fact]
    public void Value_WhenInstanceTypeIsUndefined_Throws()
    {
        var act = () => new DummyConnectionString(Options()).Value((DataStorageInstanceType)99);

        act.Should().Throw<UndefinedEnumValueException>();
    }
}
