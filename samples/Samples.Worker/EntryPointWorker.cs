using Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;
using Anarchy.Toolkit.Hosting.Host;
using Anarchy.Toolkit.Samples.Worker.Composition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Samples.Worker;

public sealed class EntryPointWorker : WorkerEntryPointBase<SampleWorkerStartup>
{
    protected override void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
    {
        builder
            .AddAwsSystemsManagerParameterStore(
                context.HostingEnvironment,
                "/samples/",
                appNamePrefix: "Anarchy.Toolkit.Samples.")
            ;
    }
}
