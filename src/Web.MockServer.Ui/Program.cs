using Anarchy.Toolkit.Web.MockServer;
using Anarchy.Toolkit.Web.MockServer.Ui.Extension;

namespace Anarchy.Toolkit.Web.MockServer.Ui;

internal static class Program
{
    public static Task<int> Main(string[] args) => MockServerEntryPoint.RunAsync(args, server => server.UseUi());
}
