using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Samples.Api.Composition.DependencyInjection;
using Anarchy.Toolkit.Samples.Api.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Web.Composition.Startup;
using Anarchy.Toolkit.Web.Extension.DependencyInjection;

namespace Anarchy.Toolkit.Samples.Api.Composition;

public sealed class Startup(IWebHostEnvironment environment, IConfiguration configuration)
    : RestApiWebAppStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
{
    protected override string ApiName => "Samples API";

    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services) =>
        base.ConfigureWebCustomServices(services)
            .UseModule(new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(), Configuration, Environment)
            .SetupAuthorization(readScope: "samples:read", writeScope: "samples:write", adminScope: "samples:admin")
            .AddSingleton<IReadOnlyCollection<IServiceHealthReport>>([]);
}
