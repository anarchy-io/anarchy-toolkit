
using Anarchy.Toolkit.Runtime.Web.MockServer.Ui.Extensions.Runtime.Web.MockServer;

namespace Anarchy.Toolkit.Runtime.Web.MockServer.Ui;

internal static class Program
{
    public static Task<int> Main(string[] args) => MockServerEntryPoint.RunAsync(args, server => server.UseUi());
}
