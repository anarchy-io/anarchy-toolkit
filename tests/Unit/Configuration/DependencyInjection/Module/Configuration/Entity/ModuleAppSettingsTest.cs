using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Entity;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Tests.Unit.Configuration.Infrastructure.Entity.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Tests.Unit.Configuration.DependencyInjection.Module.Configuration.Entity;

/// <summary>
/// Registers the test <see cref="AppSettings"/> and exposes it through every <see cref="IAppSettings"/> capability
/// interface it implements, so each section is resolvable on its own.
/// </summary>
public sealed class ModuleAppSettingsTest : ModuleAppSettingsBase<AppSettings>
{
    protected override IServiceCollection RegisterAdditionalTypesForSettings(IServiceCollection services)
    {
        return services
            .AddSingleton<IAuthServiceAppSettings>(sp => sp.GetRequiredService<AppSettings>())
            .AddSingleton<IOAuthAppSettings>(sp => sp.GetRequiredService<AppSettings>())
            .AddSingleton<IAwsAppSettings>(sp => sp.GetRequiredService<AppSettings>())
            .AddSingleton<IPostgresAppSettings>(sp => sp.GetRequiredService<AppSettings>())
            .AddSingleton<IResilienceAppSettings>(sp => sp.GetRequiredService<AppSettings>())
            .AddSingleton<IWorkerHealthCheckAppSettings>(sp => sp.GetRequiredService<AppSettings>())
            ;
    }
}
