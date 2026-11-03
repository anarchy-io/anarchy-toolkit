using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
using Anarchy.Toolkit.Hosting.Host;
using Anarchy.Toolkit.Hosting.Workers.Service.Worker;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Samples.Worker.Composition.DependencyInjection;
using Anarchy.Toolkit.Samples.Worker.Core.Service.Worker;
using Anarchy.Toolkit.Samples.Worker.Infrastructure.Entity.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Samples.Worker.Composition;

public sealed class WorkerStartup(IHostEnvironment environment, IConfiguration configuration)
    : JobStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
{
    protected override IServiceCollection ConfigureJobCustomServices(IServiceCollection services) =>
        services
            .UseModule(new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(), Configuration, Environment)
            .AddHostedService(sp => new HeartbeatWorker(sp.GetRequiredService<Observer>()))
            .AddHostedService<GreetingWorker>();
}
