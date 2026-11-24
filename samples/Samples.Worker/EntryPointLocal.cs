using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Samples.Worker;

public static class EntryPointLocal
{
    public static async Task Main(string[] args)
    {
        await using var entryPoint = new EntryPointWorker();
        await entryPoint.App.RunAsync();
    }
}
