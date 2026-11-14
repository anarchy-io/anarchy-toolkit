using Spectre.Console;

namespace Anarchy.Toolkit.Build.Cli.Core.Service.Container;

/// <summary>
/// Local container management on a Docker Compose file: start, stop, restart and destroy the environment, follow its
/// logs, and wait for containers to be running or healthy. Uses Compose V2 (<c>docker compose</c>) when available,
/// otherwise the legacy <c>docker-compose</c>; the compose file sees its own directory as <c>DOCKER_ROOT</c>.
/// </summary>
public sealed class LocalContainerTasks(
    string root,
    string composeFile,
    string projectName,
    IContainerCli? cli = null,
    IAnsiConsole? console = null,
    Action<TimeSpan>? sleep = null)
{
    /// <summary>
    /// The default seconds to wait for a container's health check to pass.
    /// </summary>
    public const int DefaultTimeout = 60;

    /// <summary>
    /// The default seconds to wait for a container to be running.
    /// </summary>
    public const int DefaultStartTimeout = 180;

    private readonly string _composeFile = Path.GetFullPath(composeFile, root);
    private readonly IContainerCli _cli = cli ?? new ProcessContainerCli(root);
    private readonly IAnsiConsole _console = console ?? AnsiConsole.Console;
    private readonly Action<TimeSpan> _sleep = sleep ?? Thread.Sleep;
    private (string Executable, string[] Prefix, string Version)? _compose;

    /// <summary>
    /// A valid Compose project name for a directory: its name in lower case, with any character other than letters,
    /// digits, dashes and underscores replaced by a dash; <c>anarchy</c> if nothing is left.
    /// </summary>
    public static string ProjectNameOf(string directory)
    {
        var name = new string(Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)))
            .ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '-').ToArray()).Trim('-', '_');
        return name.Length == 0 ? "anarchy" : name;
    }

    /// <summary>
    /// Splits extra command-line arguments on whitespace.
    /// </summary>
    public static string[] SplitArguments(string? arguments) =>
        string.IsNullOrWhiteSpace(arguments) ? [] : arguments.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// The Compose command line the tasks run, with the compose file's full path, e.g.
    /// <c>docker compose -f /repo/docker/docker-compose.yml --project-name shop</c>.
    /// </summary>
    public string ComposeCommand => string.Join(" ", [Compose.Executable, .. Compose.Prefix, .. ComposeOptions]);

    /// <summary>
    /// The Docker engine and Compose versions, the Compose command, and the running containers.
    /// </summary>
    public void Info()
    {
        var engine = _cli.Capture("docker", ["version", "--format", "{{.Server.Version}}"]);
        var grid = new Grid().AddColumn(new GridColumn().NoWrap().PadRight(2)).AddColumn();
        grid.AddRow("[green]Docker version[/]", Markup.Escape(engine.ExitCode == 0 ? engine.Output.Trim() : "unavailable"));
        grid.AddRow("[green]Docker Compose version[/]", Markup.Escape(Compose.Version));
        grid.AddRow("[green]Compose command[/]", Markup.Escape(ComposeCommand));
        _console.Write(grid);
        _console.WriteLine();
        _console.MarkupLine("[green]Currently running containers:[/]");
        if (RunningContainerIds().Count == 0) _console.MarkupLine("  [red]None[/]");
        else Require(_cli.Stream("docker", ["ps"]), "docker ps");
    }

    /// <summary>
    /// Stops every container running on the host, not only this environment's.
    /// </summary>
    public void StopAll()
    {
        var ids = RunningContainerIds();
        if (ids.Count == 0)
        {
            _console.MarkupLine("[yellow]Nothing to stop[/]");
            return;
        }
        Require(_cli.Stream("docker", ["stop", .. ids]), "docker stop");
    }

    /// <summary>
    /// Prints the environment's logs, of one container if given.
    /// </summary>
    public void Logs(string? container, IEnumerable<string> extraArguments) =>
        RunCompose(["logs", .. Optional(container), .. extraArguments]);

    /// <summary>
    /// Starts the environment in the background, then waits for each of <paramref name="waitFor"/>: to be healthy if
    /// <paramref name="healthy"/> is set (see <see cref="WaitForHealthy"/>), otherwise to be running within
    /// <paramref name="startTimeout"/> seconds.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the compose file is missing, Compose fails, or a container does not get ready in time.</exception>
    public void Start(IEnumerable<string> extraArguments, IReadOnlyCollection<string> waitFor, bool healthy,
        int timeout = DefaultTimeout, int startTimeout = DefaultStartTimeout)
    {
        RunCompose(["up", "--detach", "--quiet-pull", "--remove-orphans", .. extraArguments]);
        foreach (var container in waitFor)
        {
            if (healthy) WaitForHealthy(container, timeout, startTimeout);
            else WaitForRunning(container, startTimeout);
        }
    }

    /// <summary>
    /// Stops the environment's containers without removing them.
    /// </summary>
    public void Stop() => RunCompose(["stop"]);

    /// <summary>
    /// Restarts the environment, or one container if given.
    /// </summary>
    public void Restart(string? container) => RunCompose(["restart", .. Optional(container)]);

    /// <summary>
    /// Removes the environment's containers, networks and volumes.
    /// </summary>
    public void Destroy() => RunCompose(["down", "-v", "--remove-orphans"]);

    /// <summary>
    /// Waits up to <paramref name="timeout"/> seconds for a container to be running.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the container is not running in time.</exception>
    public void WaitForRunning(string container, int timeout = DefaultTimeout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(container);
        Wait($"Waiting for [blue]{Markup.Escape(container)}[/] to start", timeout, () =>
            Inspect(container, "{{.State.Running}}") == "true" ? WaitState.Done : WaitState.Pending,
            seconds => $"[green]{Markup.Escape(container)} is running[/] after {seconds}s",
            () => $"Container '{container}' did not start within {timeout} seconds.");
    }

    /// <summary>
    /// Waits up to <paramref name="startTimeout"/> seconds for a container to be running, then up to
    /// <paramref name="timeout"/> seconds for its health check to pass. Fails at once if it turns unhealthy or has no
    /// health check.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the container is not running or healthy in time, turns unhealthy, or has no health check.
    /// </exception>
    public void WaitForHealthy(string container, int timeout = DefaultTimeout, int startTimeout = DefaultStartTimeout)
    {
        WaitForRunning(container, startTimeout);
        string? status = null;
        Wait($"Waiting for [blue]{Markup.Escape(container)}[/] to become healthy", timeout, () => {
            status = Inspect(container, "{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}");
            return status switch {
                "healthy" => WaitState.Done,
                "starting" => WaitState.Pending,
                "unhealthy" => throw new InvalidOperationException($"Container '{container}' is unhealthy."),
                "none" => throw new InvalidOperationException($"Container '{container}' has no health check; wait for it to be running instead."),
                _ => throw new InvalidOperationException($"Container '{container}' has an unknown health status '{status}'.")
            };
        },
            seconds => $"[green]{Markup.Escape(container)} is healthy[/] after {seconds}s",
            () => $"Container '{container}' did not become healthy within {timeout} seconds (status: {status ?? "unknown"}).");
    }

    private enum WaitState { Pending, Done }

    // Checks once a second, as a progress bar filling up towards the timeout.
    private void Wait(string description, int timeout, Func<WaitState> check, Func<int, string> done, Func<string> timedOut)
    {
        if (timeout < 1) throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be at least one second.");
        var succeeded = false;
        // Spectre renders no progress at all without an interactive terminal (CI, redirected output): print plain lines.
        if (!_console.Profile.Capabilities.Interactive)
        {
            _console.MarkupLine($"{description} (timeout: {timeout}s)...");
            for (var second = 1; second <= timeout; second++)
            {
                if (check() == WaitState.Done)
                {
                    _console.MarkupLine(done(second));
                    return;
                }
                if (second < timeout) _sleep(TimeSpan.FromSeconds(1));
            }
            throw new InvalidOperationException(timedOut());
        }

        _console.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new ElapsedTimeColumn(), new SpinnerColumn())
            .Start(context => {
                var task = context.AddTask(description, maxValue: timeout);
                for (var second = 1; second <= timeout; second++)
                {
                    if (check() == WaitState.Done)
                    {
                        task.Description = done(second);
                        task.Value = task.MaxValue;
                        succeeded = true;
                        return;
                    }
                    task.Increment(1);
                    if (second < timeout) _sleep(TimeSpan.FromSeconds(1));
                }
                task.Description = $"[red]{description} timed out[/]";
                task.StopTask();
            });
        if (!succeeded) throw new InvalidOperationException(timedOut());
    }

    private string? Inspect(string container, string format)
    {
        var result = _cli.Capture("docker", ["inspect", "--format", format, container]);
        return result.ExitCode == 0 ? result.Output.Trim() : null;
    }

    private IReadOnlyList<string> RunningContainerIds()
    {
        var result = _cli.Capture("docker", ["ps", "-q"]);
        if (result.ExitCode != 0) throw new InvalidOperationException($"docker ps failed: {result.Error.Trim()}");
        return result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private (string Executable, string[] Prefix, string Version) Compose => _compose ??= DetectCompose();

    private (string Executable, string[] Prefix, string Version) DetectCompose()
    {
        var v2 = _cli.Capture("docker", ["compose", "version", "--short"]);
        if (v2.ExitCode == 0) return ("docker", ["compose"], v2.Output.Trim());
        var legacy = _cli.Capture("docker-compose", ["version", "--short"]);
        return ("docker-compose", [], legacy.ExitCode == 0 ? legacy.Output.Trim() : "unknown");
    }

    private string[] ComposeOptions => ["-f", _composeFile, "--project-name", projectName];

    private void RunCompose(string[] arguments)
    {
        if (!File.Exists(_composeFile)) throw new InvalidOperationException($"Compose file not found: {_composeFile}");
        var (executable, prefix, _) = Compose;
        var environment = new Dictionary<string, string> { ["DOCKER_ROOT"] = Path.GetDirectoryName(_composeFile)! };
        Require(_cli.Stream(executable, [.. prefix, .. ComposeOptions, .. arguments], environment), $"{executable} {string.Join(" ", [.. prefix, arguments[0]])}");
    }

    private static string[] Optional(string? value) => string.IsNullOrWhiteSpace(value) ? [] : [value];

    private static void Require(int exitCode, string command)
    {
        if (exitCode != 0) throw new InvalidOperationException($"{command} failed with exit code {exitCode}.");
    }
}
