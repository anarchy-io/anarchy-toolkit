using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;
using AwesomeAssertions;
using Dapper;
using Npgsql;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Postgres.Infrastructure.Service.Storage.Database.Postgres;

public class PostgresDbDialectTest
{
    private sealed record Input(int Id, string Name);

    private static PostgresException PostgresError(string sqlState) => new("failure", "ERROR", "ERROR", sqlState);

    [Fact]
    public void Name_IsPostgres()
    {
        PostgresDbDialect.Name.Should().Be("postgres");
    }

    [Fact]
    public void IsUniqueViolation_MatchesUniqueViolationSqlStateOnly()
    {
        PostgresDbDialect.IsUniqueViolation(PostgresError(PostgresErrorCodes.UniqueViolation)).Should().BeTrue();
        PostgresDbDialect.IsUniqueViolation(PostgresError(PostgresErrorCodes.ForeignKeyViolation)).Should().BeFalse();
        PostgresDbDialect.IsUniqueViolation(new InvalidOperationException()).Should().BeFalse();
    }

    [Fact]
    public void IsNoData_MatchesNoDataSqlStateOnly()
    {
        PostgresDbDialect.IsNoData(PostgresError(PostgresErrorCodes.NoData)).Should().BeTrue();
        PostgresDbDialect.IsNoData(PostgresError(PostgresErrorCodes.UniqueViolation)).Should().BeFalse();
    }

    [Fact]
    public void IsServerError_MatchesPostgresExceptionsButNotClientFailures()
    {
        PostgresDbDialect.IsServerError(PostgresError(PostgresErrorCodes.SyntaxError)).Should().BeTrue();
        PostgresDbDialect.IsServerError(new NpgsqlException("connection lost")).Should().BeFalse();
    }

    [Fact]
    public void ToBatchParameters_BindsOneArrayPerProperty()
    {
        var parameters = (DynamicParameters)PostgresDbDialect.ToBatchParameters(
            [new Input(1, "first"), new Input(2, "second")]);

        parameters.ParameterNames.Should().BeEquivalentTo("Id", "Name");
        parameters.Get<int[]>("Id").Should().Equal(1, 2);
        parameters.Get<string[]>("Name").Should().Equal("first", "second");
    }

    [Fact]
    public void TruncateTableSql_RestartsIdentityAndCascades()
    {
        PostgresDbDialect.TruncateTableSql("event_task_outbox")
            .Should().Be("TRUNCATE TABLE event_task_outbox RESTART IDENTITY CASCADE");
    }
}
