using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Hosting.Host;
using Anarchy.Toolkit.Samples.Job.Composition.DependencyInjection;
using Anarchy.Toolkit.Samples.Job.Core.Service;
using Anarchy.Toolkit.Samples.Job.Infrastructure.Entity.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Samples.Job.Composition;

public sealed class JobStartup(IHostEnvironment environment, IConfiguration configuration)
    : JobStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
{
    protected override IServiceCollection ConfigureJobCustomServices(IServiceCollection services) =>
        services
            .UseModule(new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(), Configuration, Environment)
            .AddScoped<GreetingService>();
}
