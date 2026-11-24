using Anarchy.Toolkit.Samples.Api.Composition;
using Anarchy.Toolkit.Web;

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
