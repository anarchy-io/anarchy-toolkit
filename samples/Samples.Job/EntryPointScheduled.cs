using Anarchy.Toolkit.Bootstrap.Aws.Extensions.Microsoft.Extensions.Configuration;
using Anarchy.Toolkit.Runtime.Aws.Host;
using Anarchy.Toolkit.Samples.Job.Composition;
using Anarchy.Toolkit.Samples.Job.Core.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Samples.Job;

/// <summary>Scheduled Lambda handler: <c>Anarchy.Toolkit.Samples.Job::Anarchy.Toolkit.Samples.Job.EntryPointScheduled::HandleAsync</c>.</summary>
public sealed class EntryPointScheduled : LambdaJobEntryPointBase<SampleJobStartup>
{
    public async Task HandleAsync()
    {
        await using var scope = BeginInvocation();
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
