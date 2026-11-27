using Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration.Entity;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Samples.Api.Infrastructure.Entity.Configuration;

namespace Anarchy.Toolkit.Samples.Api.Composition.DependencyInjection;

public sealed class ModuleAppSettings : ModuleAppSettingsBase<AppSettings>
{
    protected override IServiceCollection RegisterAdditionalTypesForSettings(IServiceCollection services) =>
        services
            .AddSingleton<IAuthServiceAppSettings>(sp => sp.GetRequiredService<AppSettings>())
            .AddSingleton<IPostgresAppSettings>(sp => sp.GetRequiredService<AppSettings>())
            .AddSingleton<IOracleAppSettings>(sp => sp.GetRequiredService<AppSettings>())
            .AddSingleton<IResilienceAppSettings>(sp => sp.GetRequiredService<AppSettings>());
}
