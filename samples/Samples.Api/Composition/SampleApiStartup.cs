using Anarchy.Toolkit.Bootstrap.Aws.DependencyInjection.Module;
using Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Core.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.DependencyInjection.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Health.Report.Mysql;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.DependencyInjection.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Health.Report.Oracle;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.DependencyInjection.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Service.Health.Report.Postgres;
using Anarchy.Toolkit.Runtime.Web.Composition.Startup;
using Anarchy.Toolkit.Runtime.Web.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Runtime.Web.Swagger.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Samples.Api.Composition.DependencyInjection;
using Anarchy.Toolkit.Samples.Api.Infrastructure.Entity.Configuration;

namespace Anarchy.Toolkit.Samples.Api.Composition;

public sealed class SampleApiStartup(IWebHostEnvironment environment, IConfiguration configuration)
    : RestApiWebAppStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
{
    protected override string ApiName => "Samples API";

    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
    {
        var settings = AppSettings;

        return PostgresHealthServiceCollectionExtensions.AddPostgresHealthReports(base.ConfigureWebCustomServices(services)
                .UseModule(new ModuleLibConfiguration<ModuleAppSettings, AppSettings>()
                    .WithSecretManagement(new ModuleAwsSecretManagement()), Configuration, Environment)
                .UseModule(new ModulePostgres(
                    _ => PostgresOptions(settings.Postgres),
                    _ => new PostgresResilienceOptions(settings.Resilience.RetryCount)), Configuration, Environment)
                .UseModule(new ModuleMysql(_ => MysqlOptions(settings.Mysql), _ => new MysqlResilienceOptions(settings.Resilience.RetryCount)),
                    Configuration, Environment)
                .UseModule(new ModuleOracle(
                    _ => OracleOptions(settings.Oracle),
                    _ => new OracleResilienceOptions(settings.Resilience.RetryCount)), Configuration, Environment))
            .AddMysqlHealthReports()
            .AddOracleHealthReports()
            .AddScoped<IReadOnlyCollection<IHealthCheck>>(sp =>
            [
                sp.GetRequiredService<PostgresHealthReportReader>(),
                sp.GetRequiredService<PostgresHealthReportWriter>(),
                sp.GetRequiredService<MysqlHealthReportReader>(),
                sp.GetRequiredService<MysqlHealthReportWriter>(),
                sp.GetRequiredService<OracleHealthReportReader>(),
                sp.GetRequiredService<OracleHealthReportWriter>(),
            ])
            .SetupAuthorization(readScope: "samples:read", writeScope: "samples:write", adminScope: "samples:admin")
            .AddToolkitSwagger(ApiName, AppSettings, Environment);
    }

    private const int CommandTimeoutSeconds = 30;

    private static PostgresConnectionOptions PostgresOptions(DbConnectionSettings db) =>
        new(db.HostReader, db.HostWriter, db.Username, db.Password, db.DbName, db.TimeZone, db.Port, CommandTimeoutSeconds);

    private static MysqlConnectionOptions MysqlOptions(DbConnectionSettings db) =>
        new(db.HostReader, db.HostWriter, db.Username, db.Password, db.DbName, db.TimeZone, db.Port, CommandTimeoutSeconds);

    private static OracleConnectionOptions OracleOptions(DbConnectionSettings db) =>
        new(db.HostReader, db.HostWriter, db.Username, db.Password, db.DbName, db.TimeZone, db.Port, CommandTimeoutSeconds);
}
