using Anarchy.Toolkit.Testing.MockServer.Client;
using Anarchy.Toolkit.Web.MockServer.Service;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Anarchy.Toolkit.Testing.MockServer.Fixture;

/// <summary>
/// How <see cref="MockServerProcess"/> starts a mock server.
/// </summary>
public sealed class MockServerProcessOptions
{
    /// <summary>
    /// A project to <c>dotnet run</c> (a host of your own built on <c>MockServerEntryPoint</c>); when absent, the
    /// <c>Anarchy.Toolkit.Web.MockServer</c> assembly next to the test assembly is run.
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>The assembly to run when there is no <see cref="ProjectPath"/>.</summary>
    public string AssemblyPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Anarchy.Toolkit.Web.MockServer.dll");

    /// <summary>The port; <see langword="null"/> uses the server's default (<c>6001</c> or the next free one).</summary>
    public int? Port { get; set; }

    /// <summary>Expectation files, directories or glob patterns.</summary>
    public List<string> ExpectationPaths { get; } = [];

    /// <summary>The record file, if any.</summary>
    public string? RecordFile { get; set; }

    /// <summary>The verbosity name passed to <c>--verbosity</c>.</summary>
    public string Verbosity { get; set; } = "minimal";

    /// <summary>Stops the server after this long even if the test never disposes it.</summary>
    public TimeSpan? KillTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Extra arguments.</summary>
    public List<string> Arguments { get; } = [];

    /// <summary>How long to wait for the ready line.</summary>
    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Receives every output line of the server (e.g. <c>ITestOutputHelper.WriteLine</c>).</summary>
    public Action<string>? Output { get; set; }
}

/// <summary>
/// A mock server running in a child process, stopped (with its process tree) on dispose.
/// </summary>
/// <example>
/// <code language="csharp">
/// await using var server = await MockServerProcess.StartAsync(new MockServerProcessOptions { ExpectationPaths = { "mocks" } });
/// // point the application at server.BaseAddress …
/// await server.Client.AssertReceivedAsync("GET", "/orders/{id}");
/// </code>
/// </example>
public sealed partial class MockServerProcess : IAsyncDisposable
{
    private readonly Process _process;

    private MockServerProcess(Process process, int port, Uri baseAddress)
    {
        _process = process;
        Port = port;
        BaseAddress = baseAddress;
        Client = new MockServerClient(baseAddress);
    }

    /// <summary>The port the server listens on.</summary>
    public int Port { get; }

    /// <summary>The server's base address.</summary>
    public Uri BaseAddress { get; }

    /// <summary>A client for its control API.</summary>
    public MockServerClient Client { get; }

    /// <summary>Whether the process has exited.</summary>
    public bool HasExited => _process.HasExited;

    /// <summary>Starts the server and waits for its ready line.</summary>
    /// <param name="options">How to start it.</param>
    /// <param name="cancellationToken">Cancels the startup.</param>
    /// <returns>The running server.</returns>
    /// <exception cref="InvalidOperationException">It exited before being ready.</exception>
    /// <exception cref="TimeoutException">It was not ready within <see cref="MockServerProcessOptions.StartupTimeout"/>.</exception>
    public static async Task<MockServerProcess> StartAsync(MockServerProcessOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new MockServerProcessOptions();
        var start = new ProcessStartInfo("dotnet") {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.Environment["NO_COLOR"] = "1";

        if (options.ProjectPath is { } project)
        {
            foreach (var arg in new[] { "run", "--project", project, "--no-launch-profile", "--" }) start.ArgumentList.Add(arg);
        }
        else
        {
            start.ArgumentList.Add(options.AssemblyPath);
        }

        if (options.Port is { } port) Add(start, "--port", port.ToString());
        foreach (var path in options.ExpectationPaths) Add(start, "--expectations", path);
        if (options.RecordFile is { } record) Add(start, "--record", record);
        if (options.KillTimeout is { } timeout) Add(start, "--kill-timeout", ((int)timeout.TotalSeconds).ToString());
        Add(start, "--verbosity", options.Verbosity);
        foreach (var arg in options.Arguments) start.ArgumentList.Add(arg);

        var process = new Process { StartInfo = start, EnableRaisingEvents = true };
        var ready = new TaskCompletionSource<(int Port, Uri Url)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = new List<string>();

        void OnLine(string? line)
        {
            if (line is null) return;
            lock (output) output.Add(line);
            options.Output?.Invoke(line);
            if (ReadyLine().Match(line) is { Success: true } match)
                ready.TrySetResult((int.Parse(match.Groups["port"].Value), new Uri(match.Groups["url"].Value)));
        }

        process.OutputDataReceived += (_, e) => OnLine(e.Data);
        process.ErrorDataReceived += (_, e) => OnLine(e.Data);
        process.Exited += (_, _) => {
            process.WaitForExit();
            ready.TrySetException(new InvalidOperationException(
                $"The mock server exited with code {process.ExitCode} before being ready:\n{string.Join('\n', Snapshot(output))}"));
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            var (readyPort, url) = await ready.Task.WaitAsync(options.StartupTimeout, cancellationToken);
            return new MockServerProcess(process, readyPort, url);
        }
        catch (Exception exception)
        {
            Kill(process);
            process.Dispose();
            if (exception is TimeoutException)
                throw new TimeoutException($"The mock server was not ready within {options.StartupTimeout}:\n{string.Join('\n', Snapshot(output))}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        Kill(_process);
        await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)).ContinueWith(_ => { }, TaskScheduler.Default);
        _process.Dispose();
    }

    private static void Add(ProcessStartInfo start, string name, string value)
    {
        start.ArgumentList.Add(name);
        start.ArgumentList.Add(value);
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Already exited.
        }
    }

    private static List<string> Snapshot(List<string> output)
    {
        lock (output) return output.ToList();
    }

    [GeneratedRegex($@"^{MockServerConsole.ReadyPrefix} port=(?<port>\d+) url=(?<url>\S+)")]
    private static partial Regex ReadyLine();
}
