using Anarchy.Toolkit.Runtime.Host;
using Anarchy.Toolkit.Samples.Job.Composition;
using Anarchy.Toolkit.Samples.Job.Core.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Anarchy.Toolkit.Samples.Job;

public static class EntryPointLocal
{
    /// <summary>Local job host, without the Lambda handler's AWS-specific configuration.</summary>
    public sealed class Job : JobEntryPointBase<SampleJobStartup>
    {
        public async Task<string> GreetAsync(string name)
        {
            await using var scope = App.Services.CreateAsyncScope();
            return await scope.ServiceProvider
                .GetRequiredService<GreetingService>()
                .GreetAsync(name)
                ;
        }
    }

    public static async Task Main(string[] args)
    {
        await using var job = new Job();
        Console.WriteLine(await job.GreetAsync(args.FirstOrDefault() ?? "world"));
    }
}
