using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Dummy;
using AwesomeAssertions;
using Moq;
using System.Data.Common;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;

public class IDbDialectTest
{
    private static string? SavepointCreate<TDialect>(string name) where TDialect : IDbDialect =>
        TDialect.SavepointCreateSql(name);

    private static string? SavepointRollback<TDialect>(string name) where TDialect : IDbDialect =>
        TDialect.SavepointRollbackSql(name);

    private static string? SavepointRelease<TDialect>(string name) where TDialect : IDbDialect =>
        TDialect.SavepointReleaseSql(name);

    private static object ToBatchParameters<TDialect>(IReadOnlyList<object> inputs) where TDialect : IDbDialect =>
        TDialect.ToBatchParameters(inputs);

    private static bool IsServerError<TDialect>(Exception exception) where TDialect : IDbDialect =>
        TDialect.IsServerError(exception);

    private static string HealthCheckSql<TDialect>() where TDialect : IDbDialect => TDialect.HealthCheckSql;

    private static string TruncateTableSql<TDialect>(string table) where TDialect : IDbDialect =>
        TDialect.TruncateTableSql(table);

    [Fact]
    public void SavepointSql_Default_UsesAnsiStatements()
    {
        SavepointCreate<DummyDbDialect>("sp").Should().Be("SAVEPOINT sp");
        SavepointRollback<DummyDbDialect>("sp").Should().Be("ROLLBACK TO SAVEPOINT sp");
        SavepointRelease<DummyDbDialect>("sp").Should().Be("RELEASE SAVEPOINT sp");
    }

    [Fact]
    public void ToBatchParameters_Default_ReturnsInputsForExecuteMany()
    {
        IReadOnlyList<object> inputs = [new { Id = 1 }, new { Id = 2 }];

        ToBatchParameters<DummyDbDialect>(inputs).Should().BeSameAs(inputs);
    }

    [Fact]
    public void IsServerError_Default_MatchesDbExceptionsOnly()
    {
        IsServerError<DummyDbDialect>(new Mock<DbException>().Object).Should().BeTrue();
        IsServerError<DummyDbDialect>(new TimeoutException()).Should().BeFalse();
    }

    [Fact]
    public void HealthCheckAndTruncateSql_Default_UseAnsiStatements()
    {
        HealthCheckSql<DummyDbDialect>().Should().Be("SELECT CURRENT_TIMESTAMP");
        TruncateTableSql<DummyDbDialect>("my_table").Should().Be("TRUNCATE TABLE my_table");
    }
}
