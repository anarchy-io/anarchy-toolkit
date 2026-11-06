using Anarchy.Toolkit.Infrastructure.Persistence.Db.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Health.Report.Mysql;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Extension.Microsoft.DependencyInjection;

public static class MysqlHealthServiceCollectionExtensions
{
    public static IServiceCollection AddMysqlHealthReports(this IServiceCollection services) =>
        services.AddDbHealthReports<MysqlHealthReportReader, MysqlHealthReportWriter>();
}
