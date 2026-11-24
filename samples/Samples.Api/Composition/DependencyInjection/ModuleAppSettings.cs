using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Entity;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
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
