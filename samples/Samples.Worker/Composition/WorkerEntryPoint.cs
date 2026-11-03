using Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;
using Anarchy.Toolkit.Hosting.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Samples.Worker.Composition;

public sealed class WorkerEntryPoint : WorkerEntryPointBase<WorkerStartup>
{
    protected override void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder) =>
        builder.AddAwsSystemsManagerParameterStore(context.HostingEnvironment, "/samples/", appNamePrefix: "Anarchy.Toolkit.Samples.");
}
