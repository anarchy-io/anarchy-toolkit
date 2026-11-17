using Anarchy.Toolkit.Web.MockServer.Service;

namespace Anarchy.Toolkit.Web.MockServer;

/// <summary>
/// Runs a mock server as a console application, configured from the command line (see
/// <see cref="MockServerArguments.Usage"/>), until Ctrl+C, SIGTERM or the kill timeout.
/// </summary>
/// <example>
/// A host of your own, with its own expectations and middlewares:
/// <code language="csharp">
/// return await MockServerEntryPoint.RunAsync(args, server => server
///     .AddExpectationsFrom("mocks")
///     .UseMiddleware&lt;FakeAuthMiddleware&gt;());
/// </code>
/// </example>
public static class MockServerEntryPoint
{
    /// <summary>Runs the mock server.</summary>
    /// <param name="args">The process arguments.</param>
    /// <param name="configure">Customizes the server: middlewares, expectations, services and options.</param>
    /// <param name="allowUnknownArguments">Ignores arguments the mock server does not know, for hosts with their own.</param>
    /// <returns>The exit code: <c>0</c> on a clean stop, <c>1</c> when it could not start, <c>2</c> on invalid arguments.</returns>
    public static async Task<int> RunAsync(string[] args, Action<MockServerBuilder>? configure = null, bool allowUnknownArguments = false)
    {
        var parsed = MockServerArguments.Parse(args, allowUnknown: allowUnknownArguments);
        var console = new MockServerConsole(parsed.Options);

        if (parsed.ShowHelp)
        {
            Console.WriteLine(MockServerArguments.Usage);
            return 0;
        }

        if (parsed.Errors.Count > 0)
        {
            foreach (var error in parsed.Errors) console.Error(error);
            Console.Error.WriteLine(MockServerArguments.Usage);
            return 2;
        }

        MockServerHost host;
        try
        {
            host = await MockServerHost.StartAsync(parsed.Options, configure);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException)
        {
            console.Error(exception.Message);
            return 1;
        }

        await using (host)
        {
            MockServerConsole.Ready(host.Port, host.BaseAddress.ToString().TrimEnd('/'));
            await host.WaitForShutdownAsync();
            host.Console.Info("Stopped.");
        }
        return 0;
    }

    /// <summary>Runs the mock server, blocking until it stops (see <see cref="RunAsync"/>).</summary>
    /// <param name="args">The process arguments.</param>
    /// <param name="configure">Customizes the server.</param>
    /// <returns>The exit code.</returns>
    public static int Run(string[] args, Action<MockServerBuilder>? configure = null) =>
        RunAsync(args, configure).GetAwaiter().GetResult();
}
