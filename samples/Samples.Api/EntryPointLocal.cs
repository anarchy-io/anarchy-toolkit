using Anarchy.Toolkit.Runtime.Web;
using Anarchy.Toolkit.Samples.Api.Composition;

namespace Anarchy.Toolkit.Samples.Api;

public static class EntryPointLocal
{
    private const string ContainerHost = "container";

    public static void Main(string[] args)
    {
        var runtimeHost = Environment.GetEnvironmentVariable("RUNTIME_HOST");

        if (string.Equals(runtimeHost, ContainerHost, StringComparison.OrdinalIgnoreCase))
        {
            WebAppEntryPoint.RunContainerWebHost<SampleApiStartup>(args);
            return;
        }

        WebAppEntryPoint.RunWebHost<SampleApiStartup>(args);
    }
}
