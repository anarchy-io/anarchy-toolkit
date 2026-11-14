using Anarchy.Toolkit.Build.Cli.Core.Service.Container;
using AwesomeAssertions;
using Spectre.Console;

namespace Anarchy.Toolkit.Tests.Unit.Build.Cli.Core.Service.Container;

public class LocalContainerTasksTest : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "anarchy-containers-" + Guid.NewGuid());
    private readonly StringWriter _output = new();
    private readonly FakeCli _cli = new();
    private readonly List<TimeSpan> _sleeps = [];

    public LocalContainerTasksTest()
    {
        Directory.CreateDirectory(Path.Combine(_root, "docker"));
        File.WriteAllText(Path.Combine(_root, "docker", "docker-compose.yml"), "services: {}\n");
    }

    public void Dispose() => Directory.Delete(_root, true);

    private LocalContainerTasks Tasks() => new(_root, "docker/docker-compose.yml", "shop", _cli,
        Console(AnsiSupport.No, InteractionSupport.No), _sleeps.Add);

    // Default enrichers detect CI (e.g. GitHub Actions) and override the ANSI and interactivity settings.
    private IAnsiConsole Console(AnsiSupport ansi, InteractionSupport interactive) => AnsiConsole.Create(new AnsiConsoleSettings {
        Out = new AnsiConsoleOutput(_output),
        Ansi = ansi,
        Interactive = interactive,
        Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false }
    });

    private string ComposeFile => Path.Combine(_root, "docker", "docker-compose.yml");

    [Theory]
    [InlineData("/work/My Shop", "my-shop")]
    [InlineData("/work/Anarchy.Toolkit/", "anarchy-toolkit")]
    [InlineData("/work/__", "anarchy")]
    public void ProjectNameOf_GivesAValidComposeProjectName(string directory, string expected) =>
        LocalContainerTasks.ProjectNameOf(directory).Should().Be(expected);

    [Fact]
    public void Compose_PrefersV2AndFallsBackToTheLegacyExecutable()
    {
        _cli.Respond("docker compose version --short", 0, "2.29.1");
        Tasks().ComposeCommand.Should().Be($"docker compose -f {ComposeFile} --project-name shop");

        _cli.Respond("docker compose version --short", 1);
        _cli.Respond("docker-compose version --short", 0, "1.29.2");
        Tasks().ComposeCommand.Should().StartWith("docker-compose -f ");
    }

    [Fact]
    public void Lifecycle_RunsComposeWithTheProjectAndDockerRoot()
    {
        _cli.Respond("docker compose version --short", 0, "2.29.1");
        _cli.Respond("docker inspect --format {{.State.Running}} db", 0, "true");
        var tasks = Tasks();

        tasks.Start(["--build"], ["db"], healthy: false);
        tasks.Logs("db", ["--tail", "5"]);
        tasks.Restart(null);
        tasks.Stop();
        tasks.Destroy();

        var prefix = $"docker compose -f {ComposeFile} --project-name shop ";
        _cli.Streamed.Should().Equal(
            prefix + "up --detach --quiet-pull --remove-orphans --build",
            prefix + "logs db --tail 5",
            prefix + "restart",
            prefix + "stop",
            prefix + "down -v --remove-orphans");
        _cli.Environments.Should().AllSatisfy(environment => environment!["DOCKER_ROOT"].Should().Be(Path.Combine(_root, "docker")));
    }

    [Fact]
    public void Lifecycle_FailsWithoutTheComposeFileOrWhenComposeFails()
    {
        _cli.Respond("docker compose version --short", 0, "2.29.1");
        _cli.StreamExitCode = 3;
        Tasks().Invoking(tasks => tasks.Stop()).Should().Throw<InvalidOperationException>().WithMessage("*exit code 3*");

        File.Delete(ComposeFile);
        Tasks().Invoking(tasks => tasks.Stop()).Should().Throw<InvalidOperationException>().WithMessage("Compose file not found*");
    }

    [Fact]
    public void StopAll_StopsEveryRunningContainerOrReportsNone()
    {
        _cli.Respond("docker ps -q", 0, "");
        Tasks().StopAll();
        _cli.Streamed.Should().BeEmpty();
        _output.ToString().Should().Contain("Nothing to stop");

        _cli.Respond("docker ps -q", 0, "abc\ndef\n");
        _cli.Streamed.Clear();
        Tasks().StopAll();
        _cli.Streamed.Should().Equal("docker stop abc def");
    }

    [Fact]
    public void WaitForRunning_PollsEverySecondUntilTheContainerRuns()
    {
        _cli.Respond("docker inspect --format {{.State.Running}} db", (1, ""), (0, "false"), (0, "true"));

        Tasks().WaitForRunning("db", timeout: 10);

        _sleeps.Should().HaveCount(2).And.AllBeEquivalentTo(TimeSpan.FromSeconds(1));
        _output.ToString().Should().Contain("db is running after 3s");
    }

    [Fact]
    public void WaitForRunning_ShowsAProgressBarOnInteractiveTerminals()
    {
        _cli.Respond("docker inspect --format {{.State.Running}} db", (1, ""), (0, "true"));
        var console = Console(AnsiSupport.Yes, InteractionSupport.Yes);

        new LocalContainerTasks(_root, ComposeFile, "shop", _cli, console, _sleeps.Add).WaitForRunning("db", timeout: 10);

        _output.ToString().Should().Contain("db is running").And.Contain("after 2s").And.Contain("━");
    }

    [Fact]
    public void WaitForRunning_FailsAfterTheTimeout()
    {
        _cli.Respond("docker inspect --format {{.State.Running}} db", 1);

        Tasks().Invoking(tasks => tasks.WaitForRunning("db", timeout: 3))
            .Should().Throw<InvalidOperationException>().WithMessage("*did not start within 3 seconds*");
        _sleeps.Should().HaveCount(2);
    }

    [Fact]
    public void WaitForHealthy_WaitsForRunningThenForTheHealthCheck()
    {
        const string health = "docker inspect --format {{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}} db";
        _cli.Respond("docker inspect --format {{.State.Running}} db", 0, "true");
        _cli.Respond(health, (0, "starting"), (0, "healthy"));

        Tasks().WaitForHealthy("db", timeout: 5, startTimeout: 5);

        _output.ToString().Should().Contain("db is running after 1s").And.Contain("db is healthy after 2s");
    }

    [Theory]
    [InlineData("unhealthy", "*is unhealthy*")]
    [InlineData("none", "*has no health check*")]
    [InlineData("", "*unknown health status*")]
    public void WaitForHealthy_FailsAtOnceUnlessHealthyOrStarting(string status, string message)
    {
        _cli.Respond("docker inspect --format {{.State.Running}} db", 0, "true");
        _cli.Respond("docker inspect --format {{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}} db", 0, status);

        Tasks().Invoking(tasks => tasks.WaitForHealthy("db", timeout: 30))
            .Should().Throw<InvalidOperationException>().WithMessage(message);
        _sleeps.Should().BeEmpty();
    }

    /// <summary>Answers captured commands from scripted responses (the last one repeats) and records streamed ones.</summary>
    private sealed class FakeCli : IContainerCli
    {
        private readonly Dictionary<string, Queue<(int, string)>> _responses = [];

        public List<string> Streamed { get; } = [];
        public List<IReadOnlyDictionary<string, string>?> Environments { get; } = [];
        public int StreamExitCode { get; set; }

        public void Respond(string command, int exitCode, string output = "") => Respond(command, (exitCode, output));

        /// <summary>Replaces the responses to a command with a sequence.</summary>
        public void Respond(string command, params (int ExitCode, string Output)[] sequence) =>
            _responses[command] = new Queue<(int, string)>(sequence);

        public (int ExitCode, string Output, string Error) Capture(string executable, IReadOnlyList<string> arguments,
            IReadOnlyDictionary<string, string>? environment = null)
        {
            var command = string.Join(" ", [executable, .. arguments]);
            if (!_responses.TryGetValue(command, out var queue) || queue.Count == 0) return (1, "", $"unexpected: {command}");
            var (exitCode, output) = queue.Count > 1 ? queue.Dequeue() : queue.Peek();
            return (exitCode, output, "");
        }

        public int Stream(string executable, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment = null)
        {
            Streamed.Add(string.Join(" ", [executable, .. arguments]));
            if (executable != "docker" || arguments[0] == "compose") Environments.Add(environment);
            return StreamExitCode;
        }
    }
}
