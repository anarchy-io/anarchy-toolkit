using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Anarchy.Toolkit.Automation.Core.Service.MockServer;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using Fallout.Solutions;
using Serilog;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Runs an <c>Anarchy.Toolkit.Runtime.Web.MockServer</c> host for integration and end-to-end tests: in the foreground
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
/// partial class Build : AnarchyBuild, IStandardBuild, IMockServerBuild
/// {
///     string[] IMockServerBuild.MockServerExpectations => ["tests/mocks"];
/// }
/// </code>
/// Then run <c>make test_integration_mocked</c>, or <c>make mock_server_start</c> and later
/// <c>make mock_server_stop</c>.
/// </example>
public interface IMockServerBuild : IStandardBuild
{
    /// <summary>
    /// The mock server host project (<c>--mock-server-project</c>), a runnable project's name; by default the only
    /// runnable project whose name ends in <c>MockServer</c>.
    /// </summary>
    [Parameter("Mock server host project (default: the runnable project whose name ends in MockServer)")]
    string? MockServerProject => TryGetValue(() => MockServerProject) ?? Preference<string>("mockServer.project");

    /// <summary>The port (<c>--mock-server-port</c>); by default <c>6001</c>, or the next free one.</summary>
    [Parameter("Mock server port (default: 6001, or the next free one)")]
    int? MockServerPort => TryGetValue<int?>(() => MockServerPort) ?? Preference<int?>("mockServer.port");

    /// <summary>Expectation files, directories or globs, relative to the repository root (<c>--mock-server-expectations</c>).</summary>
    [Parameter("Mock server expectation files, directories or globs")]
    string[] MockServerExpectations => TryGetValue(() => MockServerExpectations) ?? Preference<string[]>("mockServer.expectations") ?? [];

    /// <summary>
    /// The file received requests are recorded to (<c>--mock-server-record</c>); by default
    /// <c>artifacts/mock-server/requests.jsonl</c>. Pass an empty value to keep them in memory only.
    /// </summary>
    [Parameter("Mock server record file (default: artifacts/mock-server/requests.jsonl)")]
    string? MockServerRecord => TryGetValue(() => MockServerRecord) ?? Preference<string>("mockServer.record") ?? ArtifactsDirectory / "mock-server" / "requests.jsonl";

    /// <summary>
    /// The verbosity (<c>--mock-server-verbosity</c>): <c>quiet</c>, <c>minimal</c>, <c>normal</c>, <c>detailed</c> or
    /// <c>diagnostic</c>; by default <c>normal</c>.
    /// </summary>
    [Parameter("Mock server verbosity: quiet, minimal, normal, detailed or diagnostic (default: normal)")]
    string MockServerVerbosity => TryGetValue(() => MockServerVerbosity) ?? Preference<string>("mockServer.verbosity") ?? "normal";

    /// <summary>
    /// Whether the mock server serves its UI at <c>/__mock/ui</c> (<c>--mock-server-ui</c>), when the host plugs one in
    /// (<c>Anarchy.Toolkit.Runtime.Web.MockServer.Ui</c>); <see langword="true"/> by default, <c>false</c> passes <c>--no-ui</c>.
    /// </summary>
    [Parameter("Serve the mock server UI when the host has one (default: true)")]
    bool MockServerUi => TryGetValue<bool?>(() => MockServerUi) ?? Preference<bool?>("mockServer.ui") ?? true;

    /// <summary>Extra mock server arguments (<c>--mock-server-args</c>), e.g. <c>"--watch --unmatched-status 500"</c>.</summary>
    [Parameter("Extra mock server arguments")]
    string? MockServerArgs => TryGetValue(() => MockServerArgs) ?? Preference<string>("mockServer.args");

    /// <summary>Seconds to wait for a background mock server to be ready (<c>--mock-server-timeout</c>); 60 by default.</summary>
    [Parameter("Seconds to wait for a background mock server to be ready (default: 60)")]
    int MockServerTimeout => TryGetValue<int?>(() => MockServerTimeout) ?? Preference<int?>("mockServer.timeout") ?? 60;

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

    /// <summary>The OpenAPI specification to synchronize expectations with (<c>--mock-server-open-api</c>): a file relative to the repository root, or a URL.</summary>
    [Parameter("OpenAPI specification (file or URL) to add missing mock server expectations from")]
    string? MockServerOpenApi => TryGetValue(() => MockServerOpenApi) ?? Preference<string>("mockServer.openApi");

    /// <summary>
    /// The directory the synchronized expectations are added to (<c>--mock-server-open-api-output</c>); by default the
    /// first <see cref="MockServerExpectations"/> entry that is a directory, otherwise <c>mocks</c> under <see cref="IDefaultBuild.TestsDirectory"/>.
    /// </summary>
    [Parameter("Directory to add the expectations to (default: the first expectations directory, else tests/mocks)")]
    string? MockServerOpenApiOutput => TryGetValue(() => MockServerOpenApiOutput) ?? Preference<string>("mockServer.openApiOutput");

    /// <summary>Only reports what <see cref="MockServerSyncOpenApi"/> would add (<c>--mock-server-open-api-dry-run</c>).</summary>
    [Parameter("Only report what would be added")]
    bool MockServerOpenApiDryRun => TryGetValue<bool?>(() => MockServerOpenApiDryRun) ?? false;

    /// <summary>
    /// Adds expectations for the endpoints of <see cref="MockServerOpenApi"/> that have none, for review before
    /// committing. It never changes existing expectations: endpoints that changed or disappeared are only reported.
    /// </summary>
    Target MockServerSyncOpenApi => d => d
        .Description("Adds mock server expectations for new OpenAPI endpoints; reports changed or removed ones")
        .Requires(() => !string.IsNullOrWhiteSpace(MockServerOpenApi))
        .Executes(async () => {
            var specification = Uri.TryCreate(MockServerOpenApi, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
                ? MockServerOpenApi!
                : Path.GetFullPath(MockServerOpenApi!, RootDirectory);
            var output = Path.GetFullPath(MockServerOpenApiOutput
                ?? MockServerExpectations.FirstOrDefault(path => Directory.Exists(Path.GetFullPath(path, RootDirectory)))
                ?? TestsDirectory / "mocks", RootDirectory);

            var report = await OpenApiExpectationSync.SyncAsync(specification, output, MockServerOpenApiDryRun);

            foreach (var warning in report.Warnings) Log.Warning("{Warning}", warning);
            foreach (var added in report.Added)
                Log.Information("{Action} {Method} {Path} → {StatusCode} as '{Id}' in {File}", MockServerOpenApiDryRun ? "Would add" : "Added",
                    added.Method, added.Path, added.StatusCode, added.Id, DisplayPath(added.File));
            foreach (var notice in report.Notices)
                Log.Warning("{Kind}: {Message} ({File})", notice.Kind, notice.Message, DisplayPath(notice.File));

            Log.Information("{Count} expectation(s) {Verb}, {Notices} notice(s) to review; existing expectations were not changed",
                report.Added.Count, MockServerOpenApiDryRun ? "would be added" : "added", report.Notices.Count);
        });

    /// <summary>Runs the <c>Integration</c> tests with a mock server, started for them unless one is already running.</summary>
    Target TestIntegrationWithMockServer => d => d
        .Description("Runs test projects: Integration, with the mock server")
        .DependsOn(Compile)
        .TryDependsOn<ITestEnvironmentBuild>(x => x.PrepareTestEnvironment)
        .Executes(() => WithMockServer(() => RunTestsOfKind(TestKind.Integration)));

    /// <summary>Runs the <c>EndToEnd</c> tests with a mock server, started for them unless one is already running.</summary>
    Target TestEndToEndWithMockServer => d => d
        .Description("Runs test projects: EndToEnd, with the mock server")
        .DependsOn(Compile)
        .TryDependsOn<ITestEnvironmentBuild>(x => x.PrepareTestEnvironment)
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
        MockServerArgs,
        MockServerUi);

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

    private string DisplayPath(string path) =>
        Path.GetRelativePath(RootDirectory, path) is var relative && !relative.StartsWith("..") ? relative : path;

    private static string Quote(string argument) => argument.Contains(' ') ? $"\"{argument}\"" : argument;
}
