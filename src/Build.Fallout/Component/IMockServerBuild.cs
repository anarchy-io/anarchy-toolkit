using Anarchy.Toolkit.Build.Fallout.Component.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using Fallout.Solutions;
using Serilog;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Anarchy.Toolkit.Build.Fallout.Component;

/// <summary>
/// Runs an <c>Anarchy.Toolkit.Web.MockServer</c> host for integration and end-to-end tests: in the foreground
/// (<see cref="MockServerRun"/>), in the background (<see cref="MockServerStart"/>/<see cref="MockServerStop"/>), or
/// around a test run (<see cref="TestIntegrationWithMockServer"/>, <see cref="TestEndToEndWithMockServer"/>), which
/// passes its address to the tests as <c>MOCKSERVER_URL</c>.
/// </summary>
/// <remarks>
/// The host is a runnable project in the solution calling <c>MockServerEntryPoint.RunAsync(args)</c>, where consumer
/// middlewares and expectations are plugged in. It is launched by path, so nothing here references it.
/// </remarks>
/// <example>
/// <code language="csharp">
/// partial class Build : FalloutBuild, IStandardBuild, IMockServerBuild
/// {
///     string[] IMockServerBuild.MockServerExpectations => ["tests/mocks"];
/// }
/// </code>
/// Then run <c>fallout test-integration-with-mock-server</c>, or <c>fallout mock-server-start</c> and later
/// <c>fallout mock-server-stop</c>.
/// </example>
public interface IMockServerBuild : IStandardBuild
{
    /// <summary>
    /// The mock server host project (<c>--mock-server-project</c>), a runnable project's name; by default the only
    /// runnable project whose name ends in <c>MockServer</c>.
    /// </summary>
    [Parameter("Mock server host project (default: the runnable project whose name ends in MockServer)")]
    string? MockServerProject => TryGetValue(() => MockServerProject);

    /// <summary>The port (<c>--mock-server-port</c>); by default <c>6000</c>, or the next free one.</summary>
    [Parameter("Mock server port (default: 6000, or the next free one)")]
    int? MockServerPort => TryGetValue<int?>(() => MockServerPort);

    /// <summary>Expectation files, directories or globs, relative to the repository root (<c>--mock-server-expectations</c>).</summary>
    [Parameter("Mock server expectation files, directories or globs")]
    string[] MockServerExpectations => TryGetValue(() => MockServerExpectations) ?? [];

    /// <summary>
    /// The file received requests are recorded to (<c>--mock-server-record</c>); by default
    /// <c>artifacts/mock-server/requests.jsonl</c>. Pass an empty value to keep them in memory only.
    /// </summary>
    [Parameter("Mock server record file (default: artifacts/mock-server/requests.jsonl)")]
    string? MockServerRecord => TryGetValue(() => MockServerRecord) ?? ArtifactsDirectory / "mock-server" / "requests.jsonl";

    /// <summary>
    /// The verbosity (<c>--mock-server-verbosity</c>): <c>quiet</c>, <c>minimal</c>, <c>normal</c>, <c>detailed</c> or
    /// <c>diagnostic</c>; by default <c>normal</c>.
    /// </summary>
    [Parameter("Mock server verbosity: quiet, minimal, normal, detailed or diagnostic (default: normal)")]
    string MockServerVerbosity => TryGetValue(() => MockServerVerbosity) ?? "normal";

    /// <summary>Extra mock server arguments (<c>--mock-server-args</c>), e.g. <c>"--watch --unmatched-status 500"</c>.</summary>
    [Parameter("Extra mock server arguments")]
    string? MockServerArgs => TryGetValue(() => MockServerArgs);

    /// <summary>Seconds to wait for a background mock server to be ready (<c>--mock-server-timeout</c>); 60 by default.</summary>
    [Parameter("Seconds to wait for a background mock server to be ready (default: 60)")]
    int MockServerTimeout => TryGetValue<int?>(() => MockServerTimeout) ?? 60;

    /// <summary>Where the background mock server's state is kept.</summary>
    AbsolutePath MockServerStateFile => TemporaryDirectory / "mock-server" / "state.json";

    /// <summary>Where the background mock server's output goes.</summary>
    AbsolutePath MockServerLogFile => ArtifactsDirectory / "mock-server" / "mock-server.log";

    /// <summary>Runs the mock server in the foreground, with colours, until Ctrl+C.</summary>
    Target MockServerRun => d => d
        .Description("Runs the mock server in the foreground until Ctrl+C")
        .Executes(() => {
            var project = ResolveMockServerProject();
            Log.Information("Running mock server {Project}", project.Name);
            DotNetRun(s => s
                .SetProjectFile(project.Path)
                .SetConfiguration(Configuration)
                .SetNoLaunchProfile(true)
                .SetApplicationArguments(string.Join(' ', MockServerArguments().Select(Quote)))
                .SetVerbosity(ToolVerbosity));
        });

    /// <summary>Starts the mock server in the background and waits until it is ready.</summary>
    Target MockServerStart => d => d
        .Description("Starts the mock server in the background")
        .DependsOn(Compile)
        .Executes(() => StartMockServer());

    /// <summary>Stops the background mock server.</summary>
    Target MockServerStop => d => d
        .Description("Stops the background mock server")
        .Executes(() => {
            if (MockServerTasks.Stop(MockServerStateFile) is { } state)
                Log.Information("Stopped the mock server at {Url} (process {ProcessId})", state.Url, state.ProcessId);
            else
                Log.Warning("No mock server is running");
        });

    /// <summary>Runs the <c>Integration</c> tests with a mock server, started for them unless one is already running.</summary>
    Target TestIntegrationWithMockServer => d => d
        .Description("Runs test projects: Integration, with the mock server")
        .DependsOn(Compile)
        .Executes(() => WithMockServer(() => RunTestsOfKind(TestKind.Integration)));

    /// <summary>Runs the <c>EndToEnd</c> tests with a mock server, started for them unless one is already running.</summary>
    Target TestEndToEndWithMockServer => d => d
        .Description("Runs test projects: EndToEnd, with the mock server")
        .DependsOn(Compile)
        .Executes(() => WithMockServer(() => RunTestsOfKind(TestKind.EndToEnd)));

    /// <summary>
    /// Runs <paramref name="action"/> with <c>MOCKSERVER_URL</c> set to a mock server: the running one, or one started
    /// for it and stopped afterwards (its output is in <see cref="MockServerLogFile"/>).
    /// </summary>
    /// <param name="action">What to run, e.g. tests.</param>
    void WithMockServer(Action action)
    {
        var running = MockServerTasks.Read(MockServerStateFile);
        var state = running ?? StartMockServer();
        var previous = Environment.GetEnvironmentVariable("MOCKSERVER_URL");
        Environment.SetEnvironmentVariable("MOCKSERVER_URL", state.Url);
        try
        {
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("MOCKSERVER_URL", previous);
            if (running is null)
            {
                MockServerTasks.Stop(MockServerStateFile);
                Log.Information("Stopped the mock server; its output is in {LogFile}", Path.GetRelativePath(RootDirectory, MockServerLogFile));
            }
        }
    }

    /// <summary>The mock server's arguments, from the <c>MockServer*</c> parameters.</summary>
    /// <returns>The arguments.</returns>
    IReadOnlyList<string> MockServerArguments() => MockServerTasks.Arguments(
        MockServerPort,
        MockServerExpectations.Select(path => Path.GetFullPath(path, RootDirectory)),
        string.IsNullOrWhiteSpace(MockServerRecord) ? null : Path.GetFullPath(MockServerRecord, RootDirectory),
        MockServerVerbosity,
        MockServerArgs);

    /// <summary>The mock server host project (see <see cref="MockServerProject"/>).</summary>
    /// <returns>The project.</returns>
    /// <exception cref="Exception">There is none, or several candidates.</exception>
    Project ResolveMockServerProject()
    {
        if (!string.IsNullOrWhiteSpace(MockServerProject)) return ResolveRunnableProjectOrFail(MockServerProject);

        var candidates = GetRunnableProjects().Where(p => p.Name.EndsWith("MockServer", StringComparison.OrdinalIgnoreCase)).ToArray();
        return candidates.Length switch {
            1 => candidates[0],
            0 => throw new Exception("No mock server host found: add a runnable project whose name ends in MockServer and whose Program calls MockServerEntryPoint.RunAsync(args), or pass --mock-server-project."),
            _ => throw new Exception($"Several mock server hosts found ({string.Join(", ", candidates.Select(p => p.Name))}); pass --mock-server-project."),
        };
    }

    private MockServerState StartMockServer()
    {
        var project = ResolveMockServerProject();
        Log.Information("Starting mock server {Project} in the background", project.Name);
        var state = MockServerTasks.Start(
            project.Path, Configuration, MockServerArguments(), RootDirectory, MockServerStateFile, MockServerLogFile,
            TimeSpan.FromSeconds(MockServerTimeout));
        Log.Information("Mock server ready at {Url} (process {ProcessId}); output in {LogFile}", state.Url, state.ProcessId, Path.GetRelativePath(RootDirectory, MockServerLogFile));
        return state;
    }

    private static string Quote(string argument) => argument.Contains(' ') ? $"\"{argument}\"" : argument;
}
