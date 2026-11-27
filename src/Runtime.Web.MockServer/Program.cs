namespace Anarchy.Toolkit.Runtime.Web.MockServer;

internal static class Program
{
    public static Task<int> Main(string[] args) => MockServerEntryPoint.RunAsync(args);
}
