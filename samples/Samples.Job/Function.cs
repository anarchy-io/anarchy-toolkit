using Anarchy.Toolkit.Configuration.Extension.Microsoft.Configuration;
using Anarchy.Toolkit.Hosting.Host;
using Anarchy.Toolkit.Samples.Job.Composition;
using Anarchy.Toolkit.Samples.Job.Core.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Samples.Job;

/// <summary>
/// AWS Lambda handler (<c>Anarchy.Toolkit.Samples.Job::Anarchy.Toolkit.Samples.Job.Function::HandleAsync</c>).
/// </summary>
public sealed class Function : JobEntryPointBase<JobStartup>
{
    public async Task<string> HandleAsync(string name)
    {
        using var scope = App.Services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<GreetingService>().GreetAsync(name);
    }

    protected override void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder) =>
        builder.AddAwsSystemsManagerParameterStore(context.HostingEnvironment, "/samples/", appNamePrefix: "Anarchy.Toolkit.Samples.");
}
