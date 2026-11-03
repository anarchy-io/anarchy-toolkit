using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Entity;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Samples.Api.Infrastructure.Entity.Configuration;

namespace Anarchy.Toolkit.Samples.Api.Composition.DependencyInjection;

public sealed class ModuleAppSettings : ModuleAppSettingsBase<AppSettings>
{
    protected override IServiceCollection RegisterAdditionalTypesForSettings(IServiceCollection services) =>
        services.AddSingleton<IAuthServiceAppSettings>(sp => sp.GetRequiredService<AppSettings>());
}
