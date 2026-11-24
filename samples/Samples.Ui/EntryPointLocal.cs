using Anarchy.Toolkit.Samples.Ui.Composition;
using Anarchy.Toolkit.Web;

namespace Anarchy.Toolkit.Samples.Ui;

public static class EntryPointLocal
{
    private const string ContainerHost = "container";

    public static void Main(string[] args)
    {
        var runtimeHost = Environment.GetEnvironmentVariable("RUNTIME_HOST");

        if (string.Equals(runtimeHost, ContainerHost, StringComparison.OrdinalIgnoreCase))
        {
            WebAppEntryPoint.RunContainerWebHost<SampleUiStartup>(args);
            return;
        }

        WebAppEntryPoint.RunWebHost<SampleUiStartup>(args, 5100, 5101);
    }
}
