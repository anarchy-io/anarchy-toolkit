using Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;
using Anarchy.Toolkit.Hosting.Host;
using Anarchy.Toolkit.Samples.Job.Composition;
using Anarchy.Toolkit.Samples.Job.Core.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Samples.Job;

/// <summary>Scheduled Lambda handler: <c>Anarchy.Toolkit.Samples.Job::Anarchy.Toolkit.Samples.Job.EntryPointScheduled::HandleAsync</c>.</summary>
public sealed class EntryPointScheduled : JobEntryPointBase<SampleJobStartup>
{
    public async Task HandleAsync()
    {
        using var scope = App.Services.CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<GreetingService>()
            .GreetAsync("world")
            ;
    }

    protected override void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
    {
        builder
            .AddAwsSystemsManagerParameterStore(
                context.HostingEnvironment,
                "/samples/",
                appNamePrefix: "Anarchy.Toolkit.Samples."
            )
            ;
    }
}
