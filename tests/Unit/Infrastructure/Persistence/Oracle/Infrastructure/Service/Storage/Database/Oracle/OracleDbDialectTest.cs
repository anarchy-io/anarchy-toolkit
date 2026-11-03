using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Oracle.Fixture;
using AwesomeAssertions;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Oracle.Infrastructure.Service.Storage.Database.Oracle;

public class OracleDbDialectTest
{
    private static string? SavepointCreate<TDialect>(string name) where TDialect : IDbDialect =>
        TDialect.SavepointCreateSql(name);

    private static string? SavepointRollback<TDialect>(string name) where TDialect : IDbDialect =>
        TDialect.SavepointRollbackSql(name);

    private static string? SavepointRelease<TDialect>(string name) where TDialect : IDbDialect =>
        TDialect.SavepointReleaseSql(name);

    private static bool CanSelectFromInsert<TDialect>() where TDialect : IDbDialect => TDialect.CanSelectFromInsert;

    private static object ToBatchParameters<TDialect>(IReadOnlyList<object> inputs) where TDialect : IDbDialect =>
        TDialect.ToBatchParameters(inputs);

    private static string HealthCheckSql<TDialect>() where TDialect : IDbDialect => TDialect.HealthCheckSql;

    private static string TruncateTableSql<TDialect>(string table) where TDialect : IDbDialect =>
        TDialect.TruncateTableSql(table);

    [Fact]
    public void Name_IsOracle()
    {
        OracleDbDialect.Name.Should().Be("oracle");
    }

    [Fact]
    public void IsUniqueViolation_MatchesOra00001Only()
    {
        OracleDbDialect.IsUniqueViolation(OracleExceptionFixture.Create(1)).Should().BeTrue();
        OracleDbDialect.IsUniqueViolation(OracleExceptionFixture.Create(2291)).Should().BeFalse();
        OracleDbDialect.IsUniqueViolation(new InvalidOperationException()).Should().BeFalse();
    }

    [Fact]
    public void IsNoData_MatchesOra01403Only()
    {
        OracleDbDialect.IsNoData(OracleExceptionFixture.Create(1403)).Should().BeTrue();
        OracleDbDialect.IsNoData(OracleExceptionFixture.Create(1)).Should().BeFalse();
    }

    [Fact]
    public void IsServerError_MatchesStatementErrorsButNotLostSessions()
    {
        // ORA-00942: table or view does not exist
        OracleDbDialect.IsServerError(OracleExceptionFixture.Create(942)).Should().BeTrue();
        // ORA-03113: end-of-file on communication channel
        OracleDbDialect.IsServerError(OracleExceptionFixture.Create(3113)).Should().BeFalse();
        OracleDbDialect.IsServerError(new TimeoutException()).Should().BeFalse();
    }

    [Fact]
    public void Savepoints_HaveNoReleaseStatement()
    {
        SavepointCreate<OracleDbDialect>("sp").Should().Be("SAVEPOINT sp");
        SavepointRollback<OracleDbDialect>("sp").Should().Be("ROLLBACK TO SAVEPOINT sp");
        SavepointRelease<OracleDbDialect>("sp").Should().BeNull();
    }

    [Fact]
    public void GetOrInsert_RunsAsInsertThenLookup()
    {
        CanSelectFromInsert<OracleDbDialect>().Should().BeFalse();
    }

    [Fact]
    public void ToBatchParameters_UsesExecuteManyBinding()
    {
        IReadOnlyList<object> inputs = [new { Id = 1 }, new { Id = 2 }];

        ToBatchParameters<OracleDbDialect>(inputs).Should().BeSameAs(inputs);
    }

    [Fact]
    public void HealthCheckAndTruncateSql_UseOracleSyntax()
    {
        HealthCheckSql<OracleDbDialect>().Should().Be("SELECT SYSDATE FROM DUAL");
        TruncateTableSql<OracleDbDialect>("event_domain_metadata").Should().Be("TRUNCATE TABLE event_domain_metadata");
    }
}
