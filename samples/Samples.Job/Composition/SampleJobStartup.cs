using Anarchy.Toolkit.Bootstrap.Aws.DependencyInjection.Module;
using Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Core.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Runtime.Host;
using Anarchy.Toolkit.Samples.Job.Composition.DependencyInjection;
using Anarchy.Toolkit.Samples.Job.Core.Service;
using Anarchy.Toolkit.Samples.Job.Infrastructure.Entity.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Samples.Job.Composition;

public sealed class SampleJobStartup(IHostEnvironment environment, IConfiguration configuration)
    : JobStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
{
    protected override IServiceCollection ConfigureJobCustomServices(IServiceCollection services) =>
        services
            .UseModule(
                new ModuleLibConfiguration<ModuleAppSettings, AppSettings>()
                    .WithSecretManagement(new ModuleAwsSecretManagement()),
                Configuration, Environment)
            .AddScoped<GreetingService>();
}
